import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConversationService } from '../../../core/services/conversation.service';
import { ChatListComponent } from '../chat-list/chat-list.component';
import { ChatComponent, ChatConversation } from '../chat/chat.component';
import { ChatDetailsComponent, ConversationDetails } from '../chat-details/chat-details.component';
import { NewChatDialogComponent } from '../new-chat-dialog/new-chat-dialog.component';
import { ChatUiStateService } from '../chat-ui-state.service';

@Component({
  selector: 'app-chats',
  standalone: true,
  providers: [ChatUiStateService],
  imports: [CommonModule, ChatListComponent, ChatComponent, ChatDetailsComponent, NewChatDialogComponent],
  templateUrl: './chats.component.html',
  styleUrl: './chats.component.scss'
})
export class ChatsComponent implements OnInit {
  protected conversationService = inject(ConversationService);
  protected chatUiState = inject(ChatUiStateService);

  conversations = this.conversationService.conversations;
  loading = this.conversationService.loadingConversations;
  error = this.conversationService.errorConversations;
  showNewChatDialog = signal(false);

  ngOnInit() {
    this.conversationService.initialize();
  }

  getChatConversation(): ChatConversation | null {
    const conv = this.chatUiState.selectedConversation();
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
    const conv = this.chatUiState.selectedConversation();
    if (!conv) return null;
    
    return {
      id: conv.id,
      type: conv.type,
      name: conv.name,
      avatar: conv.avatar,
      members: conv.members
    };
  }

  onNewChat() {
    this.showNewChatDialog.set(true);
  }

  onNewChatDialogCancel() {
    this.showNewChatDialog.set(false);
  }

  onConversationCreated(conversationId: string) {
    this.conversationService.loadConversations();
    this.chatUiState.selectConversation(conversationId);
    this.showNewChatDialog.set(false);
  }

  onSendMessage(message: string) {
    // TODO: implement send message workflow
  }
}