import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { map } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';
import { GET_MY_CONVERSATIONS } from '../../../core/graphql/operations/queries';
import { Conversation, User, ConversationType } from '../../../core/graphql/generated/graphql';

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
  selector: 'app-chats',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chats.component.html',
  styleUrl: './chats.component.scss'
})
export class ChatsComponent implements OnInit {
  private apollo = inject(Apollo);
  private router = inject(Router);
  private authService = inject(AuthService);

  conversations = signal<ConversationWithLastMessage[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  selectedConversationId = signal<string | null>(null);

  currentUserId: string | null = null;

  ngOnInit() {
    const user = this.authService.currentUser();
    this.currentUserId = user?.id ?? null;
    this.loadConversations();
  }

  loadConversations() {
    this.apollo.watchQuery<{ myConversations: Conversation[] }>({
      query: GET_MY_CONVERSATIONS,
      fetchPolicy: 'network-only'
    }).valueChanges.pipe(
      map(result => result.data?.myConversations ?? [])
    ).subscribe({
      next: (conversations) => {
        const transformed: ConversationWithLastMessage[] = conversations.map(conv => ({
          id: conv.id ?? '',
          type: conv.type ?? ConversationType.Private,
          name: conv.name,
          avatar: conv.avatar,
          members: (conv.members ?? []).map(m => ({
            id: m?.id ?? '',
            username: m?.username ?? '',
            avatar: m?.avatar
          })),
          lastMessage: conv.messages && conv.messages.length > 0 && conv.messages[0] ? {
            id: conv.messages[0].id ?? '',
            content: conv.messages[0].content ?? '',
            createdAt: new Date(conv.messages[0].createdAt),
            sender: {
              id: conv.messages[0].sender?.id ?? '',
              username: conv.messages[0].sender?.username ?? ''
            }
          } : undefined
        }));
        this.conversations.set(transformed);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }

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
    this.selectedConversationId.set(conversationId);
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

  newChat() {
    // TODO: Implement new chat functionality
    console.log('New chat clicked');
  }

  getSelectedConversation(): ConversationWithLastMessage | undefined {
    const id = this.selectedConversationId();
    if (!id) return undefined;
    return this.conversations().find(c => c.id === id);
  }

  getOtherMember(conversation: ConversationWithLastMessage): { id: string; username: string; avatar?: string | null } | undefined {
    if (conversation.type === ConversationType.Private && this.currentUserId) {
      return conversation.members.find(m => m.id !== this.currentUserId);
    }
    return undefined;
  }
}