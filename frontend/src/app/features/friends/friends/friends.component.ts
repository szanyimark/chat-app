import { Component, inject, signal, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Apollo } from 'apollo-angular';
import { Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';
import { FriendRequestService } from '../../../core/services/friend-request.service';
import { GET_MY_FRIENDS } from '../../../core/graphql/operations/queries';
import { User } from '../../../core/graphql/generated/graphql';
import { AddFriendComponent } from '../add-friend/add-friend.component';

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
  protected friendRequestService = inject(FriendRequestService);
  private destroy$ = new Subject<void>();

  friends = signal<FriendWithStatus[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  activeTab = signal<'friends' | 'requests'>('friends');

  currentUserId: string | null = null;

  ngOnInit() {
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
    return this.friendRequestService.pendingIncoming;
  }

  get pendingOutgoingRequests(): FriendWithStatus[] {
    return this.friendRequestService.pendingOutgoing;
  }

  get pendingRequestsCount(): number {
    return this.pendingIncomingRequests.length;
  }

  get existingFriendIds(): string[] {
    return this.friends().map(friend => friend.id);
  }

  setActiveTab(tab: 'friends' | 'requests') {
    this.activeTab.set(tab);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  acceptRequest(friendId: string) {
    // TODO: Implement accept friend request
    console.log('Accept request:', friendId);
  }

  rejectRequest(friendId: string) {
    // TODO: Implement reject friend request
    console.log('Reject request:', friendId);
  }

  removeFriend(friendId: string) {
    // TODO: Implement remove friend
    console.log('Remove friend:', friendId);
  }

  onFriendRequestSent() {
    this.friendRequestService.loadFriendRequests();
  }

  private initializeForCurrentUser(userId: string) {
    this.currentUserId = userId;
    this.loadFriends();
  }
}