import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Apollo } from 'apollo-angular';
import { map } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';
import { GET_MY_CONVERSATIONS } from '../../../core/graphql/operations/queries';
import { Conversation, ConversationType } from '../../../core/graphql/generated/graphql';
import { ChatListComponent } from '../chat-list/chat-list.component';
import { ChatComponent, ChatConversation } from '../chat/chat.component';
import { ChatDetailsComponent, ConversationDetails } from '../chat-details/chat-details.component';

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
  imports: [CommonModule, ChatListComponent, ChatComponent, ChatDetailsComponent],
  templateUrl: './chats.component.html',
  styleUrl: './chats.component.scss'
})
export class ChatsComponent implements OnInit {
  private apollo = inject(Apollo);
  private authService = inject(AuthService);

  conversations = signal<ConversationWithLastMessage[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
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

  getSelectedConversation(): ConversationWithLastMessage | undefined {
    const id = this.selectedConversationId();
    if (!id) return undefined;
    return this.conversations().find(c => c.id === id);
  }

  getChatConversation(): ChatConversation | null {
    const conv = this.getSelectedConversation();
    if (!conv) return null;
    
    return {
      id: conv.id,
      type: conv.type,
      name: conv.name,
      avatar: conv.avatar,
      members: conv.members,
      messages: conv.lastMessage ? [{
        id: conv.lastMessage.id,
        content: conv.lastMessage.content,
        createdAt: conv.lastMessage.createdAt,
        sender: conv.lastMessage.sender
      }] : []
    };
  }

  getConversationDetails(): ConversationDetails | null {
    const conv = this.getSelectedConversation();
    if (!conv) return null;
    
    return {
      id: conv.id,
      type: conv.type,
      name: conv.name,
      avatar: conv.avatar,
      members: conv.members
    };
  }

  onConversationSelected(conversationId: string) {
    this.selectedConversationId.set(conversationId);
  }

  onNewChat() {
    console.log('New chat clicked');
  }

  onSendMessage(message: string) {
    console.log('Send message:', message);
  }
}