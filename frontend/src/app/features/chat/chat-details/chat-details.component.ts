import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConversationType } from '../../../core/graphql/generated/graphql';
import { AuthService } from '../../../core/auth/auth.service';

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

  @Input() conversation: ConversationDetails | null | undefined = null;

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  getOtherMember(): ConversationMember | undefined {
    const conv = this.conversation;
    const currentUserId = this.authService.currentUser()?.id;
    if (conv?.type === ConversationType.Private && currentUserId) {
      return conv.members.find(m => m.id !== currentUserId);
    }
    return undefined;
  }
}