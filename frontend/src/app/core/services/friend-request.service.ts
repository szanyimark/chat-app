import { Injectable, inject, signal, computed } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { Subject } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';
import { GET_FRIEND_REQUESTS } from '../graphql/operations/queries';
import { FRIEND_REQUEST_UPDATED } from '../graphql/operations/subscriptions';
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
export class FriendRequestService {
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
  
  // Emits when a friend request is accepted (signals friends list should reload)
  friendRequestAccepted$ = new Subject<void>();
  
  private subscriptionActive = false;

  initialize() {
    const user = this.authService.currentUser();
    if (!user?.id) {
      this.authService.fetchCurrentUser().subscribe({
        next: (me) => {
          if (me?.id) {
            this.startSubscription(me.id);
            this.loadFriendRequests();
          }
        }
      });
      return;
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
    if (this.subscriptionActive) {
      return;
    }

    this.subscriptionActive = true;

    this.apollo.subscribe<{ friendRequestUpdated: FriendRequest }>({
      query: FRIEND_REQUEST_UPDATED,
      variables: { userId }
    }).subscribe({
      next: (result) => {
        console.log('Subscription update received:', result.data?.friendRequestUpdated);
        this.loadFriendRequests();
        // Any friend-request update can impact the friends tab (e.g. accepted outgoing request).
        this.friendRequestAccepted$.next();
      },
      error: () => {
        this.subscriptionActive = false;
        // Retry after delay
        setTimeout(() => this.startSubscription(userId), 2000);
      },
      complete: () => {
        this.subscriptionActive = false;
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
}
