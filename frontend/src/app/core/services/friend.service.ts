import { Injectable, inject, signal, computed } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { Subject, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';
import { GET_FRIEND_REQUESTS } from '../graphql/operations/queries';
import { FRIEND_REQUEST_UPDATED, FRIENDSHIP_UPDATED } from '../graphql/operations/subscriptions';
import { FriendRequest } from '../graphql/generated/graphql';

export interface FriendRequestWithUser {
  id: string;
  friendshipId: string;
  username: string;
  avatar?: string | null;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class FriendService {
  private apollo = inject(Apollo);
  private authService = inject(AuthService);

  // State
  incomingRequests = signal<FriendRequestWithUser[]>([]);
  outgoingRequests = signal<FriendRequestWithUser[]>([]);
  loading = signal(false);
  
  // Badge count (computed from incoming requests)
  pendingCount = computed(() => 
    this.incomingRequests().filter(r => r.status === 'PENDING').length
  );
  
  // Emits when friends list should reload (request accepted, friend removed, etc.)
  friendsChanged$ = new Subject<void>();
  
  private currentSubscription: Subscription | null = null;
  private currentFriendshipSubscription: Subscription | null = null;
  private currentUserId: string | null = null;

  initialize() {
    const user = this.authService.currentUser();
    if (!user?.id) {
      this.authService.fetchCurrentUser().subscribe({
        next: (me) => {
          if (me?.id) {
            this.clearCacheAndReset();
            this.startSubscription(me.id);
            this.loadFriendRequests();
          }
        }
      });
      return;
    }

    // If switching to a different user, clear cache first
    if (this.currentUserId && this.currentUserId !== user.id) {
      this.clearCacheAndReset();
    }

    this.startSubscription(user.id);
    this.loadFriendRequests();
  }

  loadFriendRequests() {
    this.loading.set(true);

    this.apollo.query<{
      myIncomingFriendRequests: FriendRequest[];
      myOutgoingFriendRequests: FriendRequest[];
    }>({
      query: GET_FRIEND_REQUESTS,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => ({
        incoming: result.data?.myIncomingFriendRequests ?? [],
        outgoing: result.data?.myOutgoingFriendRequests ?? []
      }))
    ).subscribe({
      next: ({ incoming, outgoing }) => {
        const incomingTransformed: FriendRequestWithUser[] = incoming.map(r => ({
          friendshipId: r.id ?? '',
          id: r.fromUser?.id ?? '',
          username: r.fromUser?.username ?? '',
          avatar: r.fromUser?.avatar,
          status: r.status ?? ''
        }));

        const outgoingTransformed: FriendRequestWithUser[] = outgoing.map(r => ({
          friendshipId: r.id ?? '',
          id: r.toUser?.id ?? '',
          username: r.toUser?.username ?? '',
          avatar: r.toUser?.avatar,
          status: r.status ?? ''
        }));

        this.incomingRequests.set(incomingTransformed);
        this.outgoingRequests.set(outgoingTransformed);
        
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  private startSubscription(userId: string) {
    // If already subscribed to the same user, don't re-subscribe
    if (this.currentSubscription && this.currentUserId === userId) {
      return;
    }

    // Unsubscribe from any previous subscription
    if (this.currentSubscription) {
      this.currentSubscription.unsubscribe();
    }
    if (this.currentFriendshipSubscription) {
      this.currentFriendshipSubscription.unsubscribe();
    }

    // Clear request state when switching users
    this.incomingRequests.set([]);
    this.outgoingRequests.set([]);

    this.currentUserId = userId;

    this.currentSubscription = this.apollo.subscribe<{ friendRequestUpdated: FriendRequest }>({
      query: FRIEND_REQUEST_UPDATED,
      variables: { userId }
    }).subscribe({
      next: (result) => {
        const update = result.data?.friendRequestUpdated;
        console.log('Subscription update received:', update);
        this.loadFriendRequests();
        // Only accepted requests change the friends list.
        if (update?.status === 'ACCEPTED') {
          this.friendsChanged$.next();
        }
      },
      error: () => {
        this.currentSubscription = null;
        // Retry after delay
        setTimeout(() => this.startSubscription(userId), 2000);
      },
      complete: () => {
        this.currentSubscription = null;
        // Retry after delay
        setTimeout(() => this.startSubscription(userId), 2000);
      }
    });

    this.currentFriendshipSubscription = this.apollo.subscribe<{ friendshipUpdated: boolean }>({
      query: FRIENDSHIP_UPDATED,
      variables: { userId }
    }).subscribe({
      next: () => {
        console.log('Friendship update received');
        this.friendsChanged$.next();
      },
      error: () => {
        this.currentFriendshipSubscription = null;
        // Retry after delay
        setTimeout(() => this.startSubscription(userId), 2000);
      },
      complete: () => {
        this.currentFriendshipSubscription = null;
        // Retry after delay
        setTimeout(() => this.startSubscription(userId), 2000);
      }
    });
  }

  get pendingIncoming() {
    return this.incomingRequests().filter(r => r.status === 'PENDING');
  }

  get pendingOutgoing() {
    return this.outgoingRequests().filter(r => r.status === 'PENDING');
  }

  private clearCacheAndReset() {
    // Clear Apollo cache to prevent data pollution when switching users
    this.apollo.client.cache.reset();
    // Clear local state
    this.incomingRequests.set([]);
    this.outgoingRequests.set([]);
  }
}
