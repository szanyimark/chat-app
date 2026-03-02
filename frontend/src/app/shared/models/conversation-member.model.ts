import { User } from './user.model';
import { Conversation } from './conversation.model';

export type MemberRole = 'Member' | 'Admin';

export interface ConversationMember {
  id: string;
  conversationId: string;
  userId: string;
  role: MemberRole;
  joinedAt: string;
  user?: User;
  conversation?: Conversation;
}