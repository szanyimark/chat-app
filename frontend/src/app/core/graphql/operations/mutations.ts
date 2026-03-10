import { gql } from 'apollo-angular';

export const REGISTER = gql`
  mutation Register($input: RegisterInput!) {
    register(input: $input) {
      token
      user {
        id
        email
        username
        tag
        avatar
        createdAt
        isOnline
        lastSeenAt
      }
    }
  }
`;

export const LOGIN = gql`
  mutation Login($input: LoginInput!) {
    login(input: $input) {
      token
      user {
        id
        email
        username
        tag
        avatar
        createdAt
        isOnline
        lastSeenAt
      }
    }
  }
`;

export const CREATE_CONVERSATION = gql`
  mutation CreateConversation($input: CreateConversationInput!) {
    createConversation(input: $input) {
      id
      type
      name
      createdAt
      members {
        id
        username
        tag
        avatar
        isOnline
      }
    }
  }
`;

export const JOIN_CONVERSATION = gql`
  mutation JoinConversation($id: ID!) {
    joinConversation(id: $id) {
      id
      type
      name
      createdAt
      members {
        id
        username
        tag
        avatar
        isOnline
      }
    }
  }
`;

export const SEND_MESSAGE = gql`
  mutation SendMessage($input: SendMessageInput!) {
    sendMessage(input: $input) {
      id
      content
      createdAt
      sender {
        id
        username
        tag
        avatar
      }
      conversation {
        id
      }
    }
  }
`;

export const SEND_FRIEND_REQUEST = gql`
  mutation SendFriendRequest($userId: ID!) {
    sendFriendRequest(userId: $userId) {
      id
      status
      createdAt
      fromUser {
        id
        username
        tag
      }
      toUser {
        id
        username
        tag
      }
    }
  }
`;

export const RESPOND_TO_FRIEND_REQUEST = gql`
  mutation RespondToFriendRequest($requestId: ID!, $accept: Boolean!) {
    respondToFriendRequest(requestId: $requestId, accept: $accept)
  }
`;

export const REMOVE_FRIEND = gql`
  mutation RemoveFriend($userId: ID!) {
    removeFriend(userId: $userId)
  }
`;