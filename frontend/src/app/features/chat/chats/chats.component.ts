import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/auth/auth.service';
import { ConversationService, ConversationWithLastMessage } from '../../../core/services/conversation.service';
import { ChatListComponent } from '../chat-list/chat-list.component';
import { ChatComponent, ChatConversation } from '../chat/chat.component';
import { ChatDetailsComponent, ConversationDetails } from '../chat-details/chat-details.component';

@Component({
  selector: 'app-chats',
  standalone: true,
  imports: [CommonModule, ChatListComponent, ChatComponent, ChatDetailsComponent],
  templateUrl: './chats.component.html',
  styleUrl: './chats.component.scss'
})
export class ChatsComponent implements OnInit {
  private authService = inject(AuthService);
  protected conversationService = inject(ConversationService);

  conversations = this.conversationService.conversations;
  loading = this.conversationService.loadingConversations;
  error = this.conversationService.errorConversations;
  selectedConversationId = signal<string | null>(null);

  currentUserId: string | null = null;

  ngOnInit() {
    const user = this.authService.currentUser();
    this.currentUserId = user?.id ?? null;
    this.conversationService.initialize();
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
    // TODO: implement new chat workflow
  }

  onSendMessage(message: string) {
    // TODO: implement send message workflow
  }
}