import { Injectable, inject, signal, computed } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
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
  private allConversations = signal<ConversationWithLastMessage[]>([]);

  conversations = computed(() =>
    this.allConversations().filter(conversation =>
      conversation.type !== ConversationType.Private || conversation.members.length >= 2
    )
  );
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
    this.errorConversations.set(null);

    this.apollo.watchQuery<{ myConversations: Conversation[] }>({
      query: GET_MY_CONVERSATIONS,
      fetchPolicy: 'network-only'
    }).valueChanges.subscribe({
      next: (result) => {
        if (result.loading && !result.data) {
          return;
        }

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

        // Defensive UI dedupe: keep only one private conversation per user pair.
        const seenPrivatePairs = new Set<string>();
        const deduped = transformed.filter(conversation => {
          if (conversation.type !== ConversationType.Private || conversation.members.length !== 2) {
            return true;
          }

          const pairKey = [conversation.members[0].id, conversation.members[1].id]
            .sort()
            .join(':');

          if (seenPrivatePairs.has(pairKey)) {
            return false;
          }

          seenPrivatePairs.add(pairKey);
          return true;
        });

        this.allConversations.set(deduped);
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

    const conversations = this.allConversations();

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

  /**
   * Creates a conversation and appends it to the local list.
   * Returns an Observable that emits the new conversation's id.
   */
  createConversation(type: ConversationType, participantIds: string[], name: string | null = null): Observable<string> {
    return this.apollo.mutate<{ createConversation: Conversation }>({
      mutation: CREATE_CONVERSATION,
      variables: { input: { type, name, participantIds } }
    }).pipe(
      map(result => {
        const conversation = result.data?.createConversation;
        if (!conversation?.id) {
          throw new Error('Unable to create conversation.');
        }

        const mapped: ConversationWithLastMessage = {
          id: conversation.id,
          type: conversation.type ?? type,
          name: conversation.name,
          avatar: conversation.avatar,
          members: (conversation.members ?? []).map(member => ({
            id: member?.id ?? '',
            username: member?.username ?? '',
            avatar: member?.avatar
          })),
          lastMessage: undefined
        };

        this.allConversations.update(existing =>
          existing.some(item => item.id === mapped.id) ? existing : [mapped, ...existing]
        );

        return conversation.id;
      })
    );
  }

  private createDMConversation(friendId: string, friendUsername: string) {
    this.createConversation(ConversationType.Private, [friendId]).subscribe({
      error: (err) => console.error(`Failed to create conversation with ${friendUsername}:`, err)
    });
  }
}
