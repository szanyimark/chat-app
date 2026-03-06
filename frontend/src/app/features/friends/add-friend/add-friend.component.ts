import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, Input, Output, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Apollo } from 'apollo-angular';
import { map } from 'rxjs/operators';
import { GET_USERS } from '../../../core/graphql/operations/queries';
import { SEND_FRIEND_REQUEST } from '../../../core/graphql/operations/mutations';
import { User, SendFriendRequestMutation, SendFriendRequestMutationVariables } from '../../../core/graphql/generated/graphql';

interface SearchUser {
  id: string;
  username: string;
  tag: string;
  avatar?: string | null;
  isOnline?: boolean;
}

@Component({
  selector: 'app-add-friend',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-friend.component.html',
  styleUrl: './add-friend.component.scss'
})
export class AddFriendComponent {
  private apollo = inject(Apollo);

  @Input() currentUserId: string | null = null;
  @Input() existingFriendIds: string[] = [];

  @Output() friendRequestSent = new EventEmitter<void>();

  @ViewChild('tagInput') private tagInput?: ElementRef<HTMLInputElement>;

  isOpen = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);

  searchTag = signal('');
  searchResults = signal<SearchUser[]>([]);
  selectedUser = signal<SearchUser | null>(null);

  toggle() {
    const nextState = !this.isOpen();
    this.isOpen.set(nextState);

    if (nextState) {
      setTimeout(() => this.tagInput?.nativeElement.focus(), 0);
      return;
    }

    this.reset();
  }

  onSearchChange(value: string) {
    this.searchTag.set(value);
    this.error.set(null);

    const query = value.trim().toLowerCase();
    if (query.length < 2) {
      this.searchResults.set([]);
      this.selectedUser.set(null);
      return;
    }

    this.loading.set(true);
    this.apollo.query<{ users: User[] }>({
      query: GET_USERS,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.users ?? [])
    ).subscribe({
      next: (users) => {
        const filtered = users
          .map(u => ({
            id: u.id ?? '',
            username: u.username ?? '',
            tag: u.tag ?? '',
            avatar: u.avatar,
            isOnline: u.isOnline
          }))
          .filter(u =>
            !!u.id &&
            !!u.tag &&
            u.id !== this.currentUserId &&
            !this.existingFriendIds.includes(u.id) &&
            u.tag.toLowerCase().includes(query)
          )
          .sort((a, b) => a.tag.localeCompare(b.tag));

        this.searchResults.set(filtered);
        if (!filtered.some(u => u.id === this.selectedUser()?.id)) {
          this.selectedUser.set(null);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Unable to search users');
        this.loading.set(false);
      }
    });
  }

  selectUser(user: SearchUser) {
    this.selectedUser.set(user);
    this.searchTag.set(user.tag);
    this.searchResults.set([]);
  }

  sendRequest() {
    const user = this.selectedUser();
    if (!user) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const variables: SendFriendRequestMutationVariables = {
      userId: user.id as string
    };

    this.apollo.mutate<SendFriendRequestMutation, SendFriendRequestMutationVariables>({
      mutation: SEND_FRIEND_REQUEST,
      variables
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.friendRequestSent.emit();
        this.reset();
        this.isOpen.set(false);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Unable to send friend request');
        this.loading.set(false);
      }
    });
  }

  private reset() {
    this.loading.set(false);
    this.searchTag.set('');
    this.searchResults.set([]);
    this.selectedUser.set(null);
    this.error.set(null);
  }
}
