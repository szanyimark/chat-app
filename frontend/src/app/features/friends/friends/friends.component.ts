import { Component, inject, signal, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Apollo } from 'apollo-angular';
import { Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';
import { FriendService } from '../../../core/services/friend.service';
import { GET_MY_FRIENDS } from '../../../core/graphql/operations/queries';
import { RESPOND_TO_FRIEND_REQUEST, REMOVE_FRIEND } from '../../../core/graphql/operations/mutations';
import { User } from '../../../core/graphql/generated/graphql';
import { AddFriendComponent } from '../add-friend/add-friend.component';
import "primeicons/primeicons.css";

interface FriendWithStatus {
  id: string;
  username: string;
  avatar?: string | null;
  status?: string;
  friendshipId?: string;
}

@Component({
  selector: 'app-friends',
  standalone: true,
  imports: [CommonModule, FormsModule, AddFriendComponent],
  templateUrl: './friends.component.html',
  styleUrl: './friends.component.scss'
})
export class FriendsComponent implements OnInit, OnDestroy {
  private apollo = inject(Apollo);
  private authService = inject(AuthService);
  protected friendService = inject(FriendService);
  private destroy$ = new Subject<void>();

  friends = signal<FriendWithStatus[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  activeTab = signal<'friends' | 'requests'>('friends');

  currentUserId: string | null = null;

  ngOnInit() {
    // Listen for friends list changes (accepted requests, removals, etc.)
    this.friendService.friendsChanged$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      console.log('Friends changed event received, reloading friends');
      this.loadFriends();
    });

    const user = this.authService.currentUser();
    if (user?.id) {
      this.initializeForCurrentUser(user.id);
      return;
    }

    // On refresh, auth service may still be fetching /me. Start once that returns.
    this.authService.fetchCurrentUser().pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (me) => {
        if (me?.id) {
          this.initializeForCurrentUser(me.id);
        } else {
          this.error.set('Unable to determine current user');
          this.loading.set(false);
        }
      },
      error: (err) => {
        this.error.set(err?.message ?? 'Unable to initialize friends view');
        this.loading.set(false);
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFriends() {
    this.apollo.watchQuery<{ myFriends: User[] }>({
      query: GET_MY_FRIENDS,
      fetchPolicy: 'network-only'
    }).valueChanges.pipe(
      map(result => result.data?.myFriends ?? [])
    ).subscribe({
      next: (friends) => {
        const transformed: FriendWithStatus[] = friends.map(f => ({
          id: f.id ?? '',
          username: f.username ?? '',
          avatar: f.avatar
        }));
        this.friends.set(transformed);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }

  get filteredFriends(): FriendWithStatus[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.friends();
    return this.friends().filter(friend => 
      friend.username.toLowerCase().includes(query)
    );
  }

  get pendingIncomingRequests(): FriendWithStatus[] {
    return this.friendService.pendingIncoming;
  }

  get pendingOutgoingRequests(): FriendWithStatus[] {
    return this.friendService.pendingOutgoing;
  }

  get filteredIncomingRequests(): FriendWithStatus[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.pendingIncomingRequests;
    return this.pendingIncomingRequests.filter(request => 
      request.username.toLowerCase().includes(query)
    );
  }

  get filteredOutgoingRequests(): FriendWithStatus[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.pendingOutgoingRequests;
    return this.pendingOutgoingRequests.filter(request => 
      request.username.toLowerCase().includes(query)
    );
  }

  get pendingRequestsCount(): number {
    return this.pendingIncomingRequests.length;
  }

  get existingFriendIds(): string[] {
    return this.friends().map(friend => friend.id);
  }

  setActiveTab(tab: 'friends' | 'requests') {
    this.activeTab.set(tab);
    this.searchQuery.set('');
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  acceptRequest(requestId: string) {
    this.apollo.mutate({
      mutation: RESPOND_TO_FRIEND_REQUEST,
      variables: {
        requestId,
        accept: true
      }
    }).subscribe({
      next: () => {
        // Reload both friends and requests to reflect the change
        this.loadFriends();
        this.friendService.loadFriendRequests();
      },
      error: (err) => {
        console.error('Failed to accept friend request:', err);
        this.error.set(err.message ?? 'Failed to accept friend request');
      }
    });
  }

  rejectRequest(requestId: string) {
    this.apollo.mutate({
      mutation: RESPOND_TO_FRIEND_REQUEST,
      variables: {
        requestId,
        accept: false
      }
    }).subscribe({
      next: () => {
        // Reload requests to remove the rejected one
        this.friendService.loadFriendRequests();
      },
      error: (err) => {
        console.error('Failed to reject friend request:', err);
        this.error.set(err.message ?? 'Failed to reject friend request');
      }
    });
  }

  removeFriend(friendId: string, friendUsername: string) {
    const shouldRemove = window.confirm(`Remove ${friendUsername} from your friends?`);
    if (!shouldRemove) {
      return;
    }

    this.apollo.mutate<{ removeFriend: boolean }>({
      mutation: REMOVE_FRIEND,
      variables: { userId: friendId }
    }).subscribe({
      next: (result) => {
        if (!result.data?.removeFriend) {
          this.error.set('Unable to remove friend');
          return;
        }
        this.loadFriends();
      },
      error: (err) => {
        console.error('Failed to remove friend:', err);
        this.error.set(err.message ?? 'Failed to remove friend');
      }
    });
  }

  onFriendRequestSent() {
    this.friendService.loadFriendRequests();
  }

  private initializeForCurrentUser(userId: string) {
    this.currentUserId = userId;
    this.friendService.initialize();
    this.loadFriends();
  }
}