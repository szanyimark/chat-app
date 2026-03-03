import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  selector: 'app-chat-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-list.component.html',
  styleUrl: './chat-list.component.scss'
})
export class ChatListComponent implements OnInit {
  private apollo = inject(Apollo);
  private router = inject(Router);
  private authService = inject(AuthService);

  conversations = signal<ConversationWithLastMessage[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

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
        // Transform conversations to add last message
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

  getConversationName(conversation: ConversationWithLastMessage): string {
    // For private chats, show the other user's name
    if (conversation.type === ConversationType.Private && conversation.members && this.currentUserId) {
      const otherMember = conversation.members.find(m => m.id !== this.currentUserId);
      return otherMember?.username || 'Unknown';
    }
    // For group chats, use the conversation name
    return conversation.name || 'Unnamed Group';
  }

  getConversationAvatar(conversation: ConversationWithLastMessage): string | null | undefined {
    // For private chats, use the other user's avatar
    if (conversation.type === ConversationType.Private && conversation.members && this.currentUserId) {
      const otherMember = conversation.members.find(m => m.id !== this.currentUserId);
      return otherMember?.avatar;
    }
    // For group chats, use the conversation avatar
    return conversation.avatar;
  }

  openChat(conversationId: string) {
    this.router.navigate(['/chat', conversationId]);
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