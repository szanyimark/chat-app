import { Injectable, inject, signal } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { FriendService } from './friend.service';
import { GET_MY_CONVERSATIONS } from '../graphql/operations/queries';
import { CREATE_CONVERSATION } from '../graphql/operations/mutations';
import { Conversation, ConversationType } from '../graphql/generated/graphql';

export interface ConversationWithLastMessage {
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

@Injectable({
  providedIn: 'root'
})
export class ConversationService {
  private apollo = inject(Apollo);
  private friendService = inject(FriendService);
  private initialized = false;
  private pendingInitialFriendIds: string[] | null = null;

  conversations = signal<ConversationWithLastMessage[]>([]);
  loadingConversations = signal(true);
  errorConversations = signal<string | null>(null);

  constructor() {
    // Listen for new friends and auto-create conversations
    this.friendService.friendAdded$.subscribe(({ friendId, friendUsername }) => {
      this.ensureFriendConversation(friendId, friendUsername);
    });
  }

  initialize() {
    if (this.initialized) {
      return;
    }

    this.initialized = true;
    this.friendService.initialize();

    // One-time sync for existing friends in case events were missed previously.
    this.friendService.loadFriends(() => {
      const friendIds = this.friendService.friendIds();
      if (this.loadingConversations()) {
        this.pendingInitialFriendIds = friendIds;
      } else {
        this.ensureAllFriendConversations(friendIds);
      }
    });

    this.loadConversations();
  }

  loadConversations() {
    this.loadingConversations.set(true);

    this.apollo.watchQuery<{ myConversations: Conversation[] }>({
      query: GET_MY_CONVERSATIONS,
      fetchPolicy: 'network-only'
    }).valueChanges.subscribe({
      next: (result) => {
        const conversations = result.data?.myConversations ?? [];
        const transformed: ConversationWithLastMessage[] = conversations.map(conv => ({
          id: conv.id ?? '',
          type: conv.type ?? ConversationType.Private,
          name: conv.name,
          avatar: conv.avatar,
          members: (conv.members ?? []).map(member => ({
            id: member?.id ?? '',
            username: member?.username ?? '',
            avatar: member?.avatar
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
        this.loadingConversations.set(false);

        if (this.pendingInitialFriendIds) {
          this.ensureAllFriendConversations(this.pendingInitialFriendIds);
          this.pendingInitialFriendIds = null;
        }
      },
      error: (err) => {
        this.errorConversations.set(err.message ?? 'Failed to load conversations');
        this.loadingConversations.set(false);
      }
    });
  }

  /**
   * Ensures a DM conversation exists with the given friend.
   * If it doesn't exist, creates one.
   */
  ensureFriendConversation(friendId: string, friendUsername: string) {
    this.apollo.query<{ myConversations: Conversation[] }>({
      query: GET_MY_CONVERSATIONS
    }).subscribe({
      next: (result) => {
        const conversations = result.data?.myConversations ?? [];
        
        // Check if DM with this friend already exists
        const existingDM = conversations.find(conv => {
          // DM conversations have exactly 2 members
          if (conv.members.length !== 2) return false;
          // Check if one member is the friend
          return conv.members.some(m => m.id === friendId);
        });

        if (!existingDM) {
          // Create new DM conversation
          this.createDMConversation(friendId, friendUsername);
        }
      },
      error: (err) => {
        console.error('Failed to check existing conversations:', err);
      }
    });
  }

  /**
   * Ensures conversations exist for all given friends.
   * Used on chat-list load to handle any missed events.
   */
  ensureAllFriendConversations(friendIds: string[]) {
    if (friendIds.length === 0) return;

    const conversations = this.conversations();

    friendIds.forEach(friendId => {
      // Check if DM with this friend exists
      const existingDM = conversations.find(conv => {
        if (conv.members.length !== 2) return false;
        return conv.members.some(m => m.id === friendId);
      });

      if (!existingDM) {
        // Username is not available in bulk ensure path.
        this.createDMConversation(friendId, friendId);
      }
    });
  }

  private createDMConversation(friendId: string, friendUsername: string) {
    this.apollo.mutate<{ createConversation: Conversation }>({
      mutation: CREATE_CONVERSATION,
      variables: {
        input: {
          type: ConversationType.Private,
          name: null
        }
      }
    }).subscribe({
      next: () => {},
      error: (err) => {
        console.error(`Failed to create conversation with ${friendUsername}:`, err);
      }
    });
  }
}
