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

export const FRIEND_REQUEST_UPDATED = gql`
  subscription FriendRequestUpdated($userId: ID!) {
    friendRequestUpdated(userId: $userId) {
      id
      fromUser {
        id
        username
        tag
        avatar
        isOnline
      }
      toUser {
        id
        username
        tag
        avatar
        isOnline
      }
      status
      createdAt
      updatedAt
    }
  }
`;