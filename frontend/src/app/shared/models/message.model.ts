import { User } from './user.model';
import { Conversation } from './conversation.model';

export interface Message {
  id: string;
  content: string;
  createdAt: string;
  sender: User;
  conversation: Conversation;
}

export interface SendMessageInput {
  conversationId: string;
  content: string;
}