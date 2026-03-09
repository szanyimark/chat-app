import { gql } from 'apollo-angular';

export const GET_ME = gql`
  query GetMe {
    me {
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
`;

export const GET_USERS = gql`
  query GetUsers {
    users {
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
`;

export const SEARCH_USERS = gql`
  query SearchUsers($searchTerm: String) {
    searchUsers(searchTerm: $searchTerm) {
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
`;

export const GET_USER = gql`
  query GetUser($id: ID!) {
    user(id: $id) {
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
        tag
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
        tag
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
          tag
          avatar
        }
      }
    }
  }
`;

export const GET_MY_FRIENDS = gql`
  query GetMyFriends {
    myFriends {
      id
      username
      tag
      avatar
      isOnline
      lastSeenAt
    }
  }
`;

export const GET_FRIEND_REQUESTS = gql`
  query GetFriendRequests {
    myIncomingFriendRequests {
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
    myOutgoingFriendRequests {
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

export const GET_PENDING_FRIEND_REQUESTS = gql`
  query GetPendingFriendRequests {
    myIncomingFriendRequests {
      id
      fromUser {
        id
        username
        tag
        avatar
        isOnline
      }
      status
      createdAt
    }
  }
`;