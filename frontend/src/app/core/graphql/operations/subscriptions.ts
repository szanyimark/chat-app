import { gql } from 'apollo-angular';

export const MESSAGE_SENT = gql`
  subscription MessageSent($conversationId: ID!) {
    messageSent(conversationId: $conversationId) {
      id
      content
      createdAt
      sender {
        id
        username
        avatar
      }
      conversation {
        id
      }
    }
  }
`;

export const USER_ONLINE = gql`
  subscription UserOnline($userId: ID!) {
    userOnline(userId: $userId) {
      id
      username
      avatar
      isOnline
      lastSeenAt
    }
  }
`;