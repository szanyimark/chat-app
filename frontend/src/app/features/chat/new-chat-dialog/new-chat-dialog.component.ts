import { Component, EventEmitter, Input, Output, inject, signal, DestroyRef, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Apollo } from 'apollo-angular';
import { AuthService } from '../../../core/auth/auth.service';
import { ConversationService } from '../../../core/services/conversation.service';
import { SEARCH_USERS } from '../../../core/graphql/operations/queries';
import { ConversationType, User } from '../../../core/graphql/generated/graphql';

interface SearchUserItem {
  id: string;
  username: string;
  tag?: string | null;
}

@Component({
  selector: 'app-new-chat-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './new-chat-dialog.component.html',
  styleUrl: './new-chat-dialog.component.scss'
})
export class NewChatDialogComponent implements OnChanges {
  private apollo = inject(Apollo);
  private authService = inject(AuthService);
  private conversationService = inject(ConversationService);
  private destroyRef = inject(DestroyRef);

  @Input() visible = false;

  @ViewChild('searchInput') private searchInput?: ElementRef<HTMLInputElement>;

  @Output() cancel = new EventEmitter<void>();
  @Output() conversationCreated = new EventEmitter<string>();

  query = signal('');
  searchingUsers = signal(false);
  creatingConversation = signal(false);
  createError = signal<string | null>(null);
  searchResults = signal<SearchUserItem[]>([]);
  selectedUsers = signal<SearchUserItem[]>([]);

  private searchRequestId = 0;
  private searchSubject = new Subject<string>();

  constructor() {
    this.searchSubject
      .pipe(
        debounceTime(300),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((searchTerm: string) => this.performSearch(searchTerm));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && changes['visible'].currentValue) {
      this.focusSearchInput();
    }

    if (changes['visible'] && !changes['visible'].currentValue) {
      this.resetDialogState();
    }
  }

  closeDialog() {
    this.resetDialogState();
    this.cancel.emit();
  }

  private resetDialogState() {
    this.query.set('');
    this.searchResults.set([]);
    this.selectedUsers.set([]);
    this.searchingUsers.set(false);
    this.creatingConversation.set(false);
    this.createError.set(null);
    this.searchRequestId++;
  }

  onSearchChange(value: string) {
    this.query.set(value);
    this.createError.set(null);
    this.searchSubject.next(value);
  }

  private performSearch(value: string) {
    const trimmed = value.trim();
    if (!trimmed) {
      this.searchResults.set([]);
      this.searchingUsers.set(false);
      return;
    }

    const requestId = ++this.searchRequestId;
    this.searchingUsers.set(true);

    this.apollo.query<{ searchUsers: User[] }>({
      query: SEARCH_USERS,
      variables: { searchTerm: trimmed },
      fetchPolicy: 'network-only'
    }).subscribe({
      next: (result) => {
        if (requestId !== this.searchRequestId) {
          return;
        }

        const selectedIds = new Set(this.selectedUsers().map(user => user.id));
        const users = (result.data?.searchUsers ?? [])
          .map(user => ({
            id: user.id ?? '',
            username: user.username ?? '',
            tag: user.tag
          }))
          .filter(user =>
            !!user.id &&
            user.id !== this.authService.currentUser()?.id &&
            !selectedIds.has(user.id)
          );

        this.searchResults.set(users);
        this.searchingUsers.set(false);
      },
      error: () => {
        if (requestId !== this.searchRequestId) {
          return;
        }

        this.searchResults.set([]);
        this.searchingUsers.set(false);
      }
    });
  }

  addUserToSelection(user: SearchUserItem) {
    if (this.selectedUsers().some(selected => selected.id === user.id)) {
      return;
    }

    this.selectedUsers.update(selected => [...selected, user]);
    this.query.set('');
    this.searchResults.set([]);
    this.focusSearchInput();
  }

  private focusSearchInput() {
    // Wait for @if(visible) block to render before focusing.
    setTimeout(() => {
      this.searchInput?.nativeElement.focus();
    }, 0);
  }

  removeUserFromSelection(userId: string) {
    this.selectedUsers.update(selected => selected.filter(user => user.id !== userId));
  }

  createConversation() {
    const participants = this.selectedUsers();
    if (participants.length === 0 || this.creatingConversation()) {
      return;
    }

    const participantIds = participants.map(user => user.id);
    const type = participants.length > 1 ? ConversationType.Group : ConversationType.Private;

    this.creatingConversation.set(true);
    this.createError.set(null);

    this.conversationService.createConversation(type, participantIds).subscribe({
      next: (conversationId) => {
        this.creatingConversation.set(false);
        this.conversationCreated.emit(conversationId);
      },
      error: (err) => {
        this.creatingConversation.set(false);
        this.createError.set(err?.message ?? 'Unable to create conversation.');
      }
    });
  }
}
