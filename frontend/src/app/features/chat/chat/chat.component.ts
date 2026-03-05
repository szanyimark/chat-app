import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConversationType } from '../../../core/graphql/generated/graphql';

export interface Message {
  id: string;
  content: string;
  createdAt: Date;
  sender: {
    id: string;
    username: string;
  };
}

export interface ChatConversation {
  id: string;
  type: ConversationType;
  name?: string | null;
  avatar?: string | null;
  members: { id: string; username: string; avatar?: string | null }[];
  messages: Message[];
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent {
  @Input() conversation: ChatConversation | null | undefined = null;
  @Input() currentUserId: string | null = null;
  @Output() sendMessage = new EventEmitter<string>();

  newMessage = '';

  getConversationName(): string {
    const conv = this.conversation;
    if (!conv) return '';
    
    if (conv.type === ConversationType.Private && this.currentUserId) {
      const otherMember = conv.members.find(m => m.id !== this.currentUserId);
      return otherMember?.username || 'Unknown';
    }
    return conv.name || 'Unnamed Group';
  }

  getConversationAvatar(): string | null | undefined {
    const conv = this.conversation;
    if (!conv) return undefined;
    
    if (conv.type === ConversationType.Private && this.currentUserId) {
      const otherMember = conv.members.find(m => m.id !== this.currentUserId);
      return otherMember?.avatar;
    }
    return conv.avatar;
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  onSendMessage() {
    if (this.newMessage.trim()) {
      this.sendMessage.emit(this.newMessage.trim());
      this.newMessage = '';
    }
  }

  onKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSendMessage();
    }
  }

  formatMessageTime(date: Date): string {
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