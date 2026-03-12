import { ConversationType } from '../../core/graphql/generated/graphql';

export interface ConversationMemberLike {
  id: string;
  username: string;
  avatar?: string | null;
}

export interface ConversationLike {
  type: ConversationType;
  name?: string | null;
  avatar?: string | null;
  members: ConversationMemberLike[];
}

export function getOtherConversationMember(
  conversation: ConversationLike,
  currentUserId: string | null | undefined
): ConversationMemberLike | undefined {
  if (conversation.type !== ConversationType.Private || !currentUserId) {
    return undefined;
  }

  return conversation.members.find(member => member.id !== currentUserId);
}

export function getConversationDisplayName(
  conversation: ConversationLike,
  currentUserId: string | null | undefined
): string {
  if (conversation.type === ConversationType.Private) {
    return getOtherConversationMember(conversation, currentUserId)?.username ?? 'Unknown';
  }

  return conversation.name ?? 'Unnamed Group';
}

export function getConversationDisplayAvatar(
  conversation: ConversationLike,
  currentUserId: string | null | undefined
): string | null | undefined {
  if (conversation.type === ConversationType.Private) {
    return getOtherConversationMember(conversation, currentUserId)?.avatar;
  }

  return conversation.avatar;
}

export function getInitials(name: string): string {
  return name
    .split(' ')
    .map(word => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}
