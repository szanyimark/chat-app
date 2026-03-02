import { User } from './user.model';

export type ConversationType = 'PRIVATE' | 'GROUP';

export interface Conversation {
  id: string;
  type: ConversationType;
  name: string | null;
  members: User[];
  createdAt: string;
}

export interface CreateConversationInput {
  type: ConversationType;
  name?: string;
}