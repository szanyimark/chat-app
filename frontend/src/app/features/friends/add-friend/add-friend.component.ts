import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Apollo } from 'apollo-angular';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, map, takeUntil } from 'rxjs/operators';
import { SEARCH_USERS } from '../../../core/graphql/operations/queries';
import { SEND_FRIEND_REQUEST } from '../../../core/graphql/operations/mutations';
import { SearchUsersQuery, SendFriendRequestMutation, SendFriendRequestMutationVariables } from '../../../core/graphql/generated/graphql';

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
export class AddFriendComponent implements OnDestroy, OnInit {
  private apollo = inject(Apollo);
  private destroy$ = new Subject<void>();
  private searchSubject$ = new Subject<string>();

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

  ngOnInit() {
    // Debounce search input by 300ms to avoid querying on every keystroke
    this.searchSubject$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(query => this.executeSearch(query));
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

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

    const query = value.trim();
    if (query.length < 2) {
      this.searchResults.set([]);
      this.selectedUser.set(null);
      return;
    }

    // Push to subject for debouncing (no need to check length again in executeSearch)
    this.searchSubject$.next(query);
  }

  private executeSearch(query: string) {
    this.loading.set(true);
    
    this.apollo.query<SearchUsersQuery>({
      query: SEARCH_USERS,
      variables: { searchTerm: query },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.searchUsers ?? [])
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
            u.id !== this.currentUserId &&
            !this.existingFriendIds.includes(u.id)
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
