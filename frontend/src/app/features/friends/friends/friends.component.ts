import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Apollo } from 'apollo-angular';
import { map } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';
import { GET_MY_FRIENDS, GET_FRIEND_REQUESTS } from '../../../core/graphql/operations/queries';
import { User, FriendRequest } from '../../../core/graphql/generated/graphql';

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
  imports: [CommonModule, FormsModule],
  templateUrl: './friends.component.html',
  styleUrl: './friends.component.scss'
})
export class FriendsComponent implements OnInit {
  private apollo = inject(Apollo);
  private authService = inject(AuthService);

  friends = signal<FriendWithStatus[]>([]);
  friendRequests = signal<FriendWithStatus[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  activeTab = signal<'friends' | 'requests'>('friends');

  currentUserId: string | null = null;

  ngOnInit() {
    const user = this.authService.currentUser();
    this.currentUserId = user?.id ?? null;
    this.loadFriends();
    this.loadFriendRequests();
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

  loadFriendRequests() {
    this.apollo.watchQuery<{ friendRequests: FriendRequest[] }>({
      query: GET_FRIEND_REQUESTS,
      fetchPolicy: 'network-only'
    }).valueChanges.pipe(
      map(result => result.data?.friendRequests ?? [])
    ).subscribe({
      next: (requests) => {
        const transformed: FriendWithStatus[] = requests.map(r => ({
          id: r.fromUser?.id ?? '',
          username: r.fromUser?.username ?? '',
          avatar: r.fromUser?.avatar,
          status: r.status
        }));
        this.friendRequests.set(transformed);
      },
      error: (err) => {
        console.error('Error loading friend requests:', err);
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

  get pendingRequests(): FriendWithStatus[] {
    return this.friendRequests().filter(r => r.status === 'PENDING');
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
}