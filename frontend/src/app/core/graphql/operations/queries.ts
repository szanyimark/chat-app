import { gql } from 'apollo-angular';

export const GET_ME = gql`
  query GetMe {
    me {
      id
      email
      username
      avatar
      createdAt
      isOnline
      lastSeenAt
    }
  }
`;

export const GET_USERS = gql`
  query GetUsers {
    users {
      id
      email
      username
      avatar
      createdAt
      isOnline
      lastSeenAt
    }
  }
`;

export const GET_USER = gql`
  query GetUser($id: ID!) {
    user(id: $id) {
      id
      email
      username
      avatar
      createdAt
      isOnline
      lastSeenAt
    }
  }
`;

export const GET_MY_CONVERSATIONS = gql`
  query GetMyConversations {
    myConversations {
      id
      type
      name
      avatar
      createdAt
      members {
        id
        username
        avatar
        isOnline
      }
      messages(limit: 1) {
        id
        content
        createdAt
        sender {
          id
          username
        }
      }
    }
  }
`;

export const GET_CONVERSATION = gql`
  query GetConversation($id: ID!) {
    conversation(id: $id) {
      id
      type
      name
      avatar
      createdAt
      members {
        id
        username
        avatar
        isOnline
      }
      messages(limit: 50) {
        id
        content
        createdAt
        sender {
          id
          username
          avatar
        }
      }
    }
  }
`;