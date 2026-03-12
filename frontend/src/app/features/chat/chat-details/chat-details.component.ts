import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConversationType } from '../../../core/graphql/generated/graphql';
import { AuthService } from '../../../core/auth/auth.service';
import {
  getConversationDisplayAvatar,
  getConversationDisplayName,
  getInitials,
  getOtherConversationMember
} from '../conversation-display.util';

export interface ConversationMember {
  id: string;
  username: string;
  avatar?: string | null;
}

export interface ConversationDetails {
  id: string;
  type: ConversationType;
  name?: string | null;
  avatar?: string | null;
  members: ConversationMember[];
}

@Component({
  selector: 'app-chat-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-details.component.html',
  styleUrl: './chat-details.component.scss'
})
export class ChatDetailsComponent {
  private authService = inject(AuthService);
  readonly getInitials = getInitials;

  @Input() conversation: ConversationDetails | null | undefined = null;

  get currentUserId(): string | null {
    return this.authService.currentUser()?.id ?? null;
  }

  getConversationName(): string {
    if (!this.conversation) {
      return 'Unnamed Chat';
    }

    return getConversationDisplayName(this.conversation, this.currentUserId);
  }

  getConversationAvatar(): string | null | undefined {
    if (!this.conversation) {
      return undefined;
    }

    return getConversationDisplayAvatar(this.conversation, this.currentUserId);
  }

  getOtherMember(): ConversationMember | undefined {
    const conv = this.conversation;
    if (conv?.type === ConversationType.Private && this.currentUserId) {
      return getOtherConversationMember(conv, this.currentUserId);
    }
    return undefined;
  }
}