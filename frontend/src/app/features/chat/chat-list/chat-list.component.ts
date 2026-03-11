import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConversationType } from '../../../core/graphql/generated/graphql';
import { ConversationService } from '../../../core/services/conversation.service';
import { AuthService } from '../../../core/auth/auth.service';

interface ConversationWithLastMessage {
  id: string;
  type: ConversationType;
  name?: string | null;
  avatar?: string | null;
  members: { id: string; username: string; avatar?: string | null }[];
  lastMessage?: {
    id: string;
    content: string;
    createdAt: Date;
    sender: {
      id: string;
      username: string;
    };
  };
}

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

  conversations = this.conversationService.conversations;
  loading = this.conversationService.loadingConversations;
  error = this.conversationService.errorConversations;

  get currentUserId(): string | null {
    return this.authService.currentUser()?.id ?? null;
  }

  @Input() selectedConversationId: string | null = null;

  @Output() conversationSelected = new EventEmitter<string>();
  @Output() newChatClicked = new EventEmitter<void>();

  searchQuery = signal('');

  get filteredConversations(): ConversationWithLastMessage[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.conversations();
    return this.conversations().filter(conv => 
      this.getConversationName(conv).toLowerCase().includes(query)
    );
  }

  getConversationName(conversation: ConversationWithLastMessage): string {
    if (conversation.type === ConversationType.Private && conversation.members && this.currentUserId) {
      const otherMember = conversation.members.find(m => m.id !== this.currentUserId);
      return otherMember?.username || 'Unknown';
    }
    return conversation.name || 'Unnamed Group';
  }

  getConversationAvatar(conversation: ConversationWithLastMessage): string | null | undefined {
    if (conversation.type === ConversationType.Private && conversation.members && this.currentUserId) {
      const otherMember = conversation.members.find(m => m.id !== this.currentUserId);
      return otherMember?.avatar;
    }
    return conversation.avatar;
  }

  selectConversation(conversationId: string) {
    this.selectedConversationId = conversationId;
    this.conversationSelected.emit(conversationId);
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

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }
}