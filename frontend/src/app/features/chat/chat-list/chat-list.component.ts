import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConversationService, ConversationWithLastMessage } from '../../../core/services/conversation.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  getConversationDisplayAvatar,
  getConversationDisplayName,
  getInitials
} from '../conversation-display.util';
import { ChatUiStateService } from '../chat-ui-state.service';

@Component({
  selector: 'app-chat-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-list.component.html',
  styleUrl: './chat-list.component.scss'
})
export class ChatListComponent {
  private conversationService = inject(ConversationService);
  private authService = inject(AuthService);
  protected chatUiState = inject(ChatUiStateService);

  conversations = this.conversationService.conversations;
  loading = this.conversationService.loadingConversations;
  error = this.conversationService.errorConversations;

  get currentUserId(): string | null {
    return this.authService.currentUser()?.id ?? null;
  }

  @Output() newChatClicked = new EventEmitter<void>();

  searchQuery = signal('');
  readonly getInitials = getInitials;

  get filteredConversations(): ConversationWithLastMessage[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.conversations();
    return this.conversations().filter(conv => 
      this.getConversationName(conv).toLowerCase().includes(query)
    );
  }

  getConversationName(conversation: ConversationWithLastMessage): string {
    return getConversationDisplayName(conversation, this.currentUserId);
  }

  getConversationAvatar(conversation: ConversationWithLastMessage): string | null | undefined {
    return getConversationDisplayAvatar(conversation, this.currentUserId);
  }

  selectConversation(conversationId: string) {
    this.chatUiState.selectConversation(conversationId);
  }

  newChat() {
    this.newChatClicked.emit();
  }

  formatMessageTime(date: Date | undefined): string {
    if (!date) return '';
    
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) {
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (days === 1) {
      return 'Yesterday';
    } else if (days < 7) {
      return date.toLocaleDateString([], { weekday: 'short' });
    } else {
      return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    }
  }

}