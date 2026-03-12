import { Injectable, computed, signal, inject } from '@angular/core';
import { ConversationService } from '../../core/services/conversation.service';

@Injectable()
export class ChatUiStateService {
  private conversationService = inject(ConversationService);

  selectedConversationId = signal<string | null>(null);

  selectedConversation = computed(() => {
    const selectedId = this.selectedConversationId();
    if (!selectedId) {
      return null;
    }

    return this.conversationService
      .conversations()
      .find(conversation => conversation.id === selectedId) ?? null;
  });

  selectConversation(conversationId: string): void {
    this.selectedConversationId.set(conversationId);
  }

  clearSelection(): void {
    this.selectedConversationId.set(null);
  }
}
