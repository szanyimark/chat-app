import { gql } from 'apollo-angular';

export const REGISTER = gql`
  mutation Register($input: RegisterInput!) {
    register(input: $input) {
      token
      user {
        id
        email
        username
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
        avatar
      }
      conversation {
        id
      }
    }
  }
`;