import { gql } from 'apollo-angular';
import { Injectable } from '@angular/core';
import * as Apollo from 'apollo-angular';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  DateTime: { input: any; output: any; }
};

export type AuthPayload = {
  __typename?: 'AuthPayload';
  token: Scalars['String']['output'];
  user: User;
};

export type Conversation = {
  __typename?: 'Conversation';
  createdAt: Scalars['DateTime']['output'];
  id: Scalars['ID']['output'];
  members: Array<User>;
  messages: Array<Message>;
  name?: Maybe<Scalars['String']['output']>;
  type: ConversationType;
};


export type ConversationMessagesArgs = {
  limit?: InputMaybe<Scalars['Int']['input']>;
};

export enum ConversationType {
  Group = 'GROUP',
  Private = 'PRIVATE'
}

export type CreateConversationInput = {
  name?: InputMaybe<Scalars['String']['input']>;
  type: ConversationType;
};

export type LoginInput = {
  email: Scalars['String']['input'];
  password: Scalars['String']['input'];
};

export type Message = {
  __typename?: 'Message';
  content: Scalars['String']['output'];
  conversation: Conversation;
  createdAt: Scalars['DateTime']['output'];
  id: Scalars['ID']['output'];
  sender: User;
};

export type Mutation = {
  __typename?: 'Mutation';
  createConversation: Conversation;
  joinConversation: Conversation;
  login: AuthPayload;
  register: AuthPayload;
  sendMessage: Message;
};


export type MutationCreateConversationArgs = {
  input: CreateConversationInput;
};


export type MutationJoinConversationArgs = {
  id: Scalars['ID']['input'];
};


export type MutationLoginArgs = {
  input: LoginInput;
};


export type MutationRegisterArgs = {
  input: RegisterInput;
};


export type MutationSendMessageArgs = {
  input: SendMessageInput;
};

export type Query = {
  __typename?: 'Query';
  conversation?: Maybe<Conversation>;
  me?: Maybe<User>;
  myConversations: Array<Conversation>;
  user?: Maybe<User>;
  users: Array<User>;
};


export type QueryConversationArgs = {
  id: Scalars['ID']['input'];
};


export type QueryUserArgs = {
  id: Scalars['ID']['input'];
};

export type RegisterInput = {
  email: Scalars['String']['input'];
  password: Scalars['String']['input'];
  username: Scalars['String']['input'];
};

export type SendMessageInput = {
  content: Scalars['String']['input'];
  conversationId: Scalars['ID']['input'];
};

export type Subscription = {
  __typename?: 'Subscription';
  messageSent: Message;
  userOnline: User;
};


export type SubscriptionMessageSentArgs = {
  conversationId: Scalars['ID']['input'];
};


export type SubscriptionUserOnlineArgs = {
  userId: Scalars['ID']['input'];
};

export type User = {
  __typename?: 'User';
  avatar?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  email: Scalars['String']['output'];
  id: Scalars['ID']['output'];
  isOnline: Scalars['Boolean']['output'];
  lastSeenAt?: Maybe<Scalars['DateTime']['output']>;
  username: Scalars['String']['output'];
};

export type RegisterMutationVariables = Exact<{
  input: RegisterInput;
}>;


export type RegisterMutation = { __typename?: 'Mutation', register: { __typename?: 'AuthPayload', token: string, user: { __typename?: 'User', id: string, email: string, username: string, avatar?: string | null, createdAt: any, isOnline: boolean, lastSeenAt?: any | null } } };

export type LoginMutationVariables = Exact<{
  input: LoginInput;
}>;


export type LoginMutation = { __typename?: 'Mutation', login: { __typename?: 'AuthPayload', token: string, user: { __typename?: 'User', id: string, email: string, username: string, avatar?: string | null, createdAt: any, isOnline: boolean, lastSeenAt?: any | null } } };

export type CreateConversationMutationVariables = Exact<{
  input: CreateConversationInput;
}>;


export type CreateConversationMutation = { __typename?: 'Mutation', createConversation: { __typename?: 'Conversation', id: string, type: ConversationType, name?: string | null, createdAt: any, members: Array<{ __typename?: 'User', id: string, username: string, avatar?: string | null, isOnline: boolean }> } };

export type JoinConversationMutationVariables = Exact<{
  id: Scalars['ID']['input'];
}>;


export type JoinConversationMutation = { __typename?: 'Mutation', joinConversation: { __typename?: 'Conversation', id: string, type: ConversationType, name?: string | null, createdAt: any, members: Array<{ __typename?: 'User', id: string, username: string, avatar?: string | null, isOnline: boolean }> } };

export type SendMessageMutationVariables = Exact<{
  input: SendMessageInput;
}>;


export type SendMessageMutation = { __typename?: 'Mutation', sendMessage: { __typename?: 'Message', id: string, content: string, createdAt: any, sender: { __typename?: 'User', id: string, username: string, avatar?: string | null }, conversation: { __typename?: 'Conversation', id: string } } };

export type GetMeQueryVariables = Exact<{ [key: string]: never; }>;


export type GetMeQuery = { __typename?: 'Query', me?: { __typename?: 'User', id: string, email: string, username: string, avatar?: string | null, createdAt: any, isOnline: boolean, lastSeenAt?: any | null } | null };

export type GetUsersQueryVariables = Exact<{ [key: string]: never; }>;


export type GetUsersQuery = { __typename?: 'Query', users: Array<{ __typename?: 'User', id: string, email: string, username: string, avatar?: string | null, createdAt: any, isOnline: boolean, lastSeenAt?: any | null }> };

export type GetUserQueryVariables = Exact<{
  id: Scalars['ID']['input'];
}>;


export type GetUserQuery = { __typename?: 'Query', user?: { __typename?: 'User', id: string, email: string, username: string, avatar?: string | null, createdAt: any, isOnline: boolean, lastSeenAt?: any | null } | null };

export type GetMyConversationsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetMyConversationsQuery = { __typename?: 'Query', myConversations: Array<{ __typename?: 'Conversation', id: string, type: ConversationType, name?: string | null, createdAt: any, members: Array<{ __typename?: 'User', id: string, username: string, avatar?: string | null, isOnline: boolean }> }> };

export type GetConversationQueryVariables = Exact<{
  id: Scalars['ID']['input'];
}>;


export type GetConversationQuery = { __typename?: 'Query', conversation?: { __typename?: 'Conversation', id: string, type: ConversationType, name?: string | null, createdAt: any, members: Array<{ __typename?: 'User', id: string, username: string, avatar?: string | null, isOnline: boolean }>, messages: Array<{ __typename?: 'Message', id: string, content: string, createdAt: any, sender: { __typename?: 'User', id: string, username: string, avatar?: string | null } }> } | null };

export type MessageSentSubscriptionVariables = Exact<{
  conversationId: Scalars['ID']['input'];
}>;


export type MessageSentSubscription = { __typename?: 'Subscription', messageSent: { __typename?: 'Message', id: string, content: string, createdAt: any, sender: { __typename?: 'User', id: string, username: string, avatar?: string | null }, conversation: { __typename?: 'Conversation', id: string } } };

export type UserOnlineSubscriptionVariables = Exact<{
  userId: Scalars['ID']['input'];
}>;


export type UserOnlineSubscription = { __typename?: 'Subscription', userOnline: { __typename?: 'User', id: string, username: string, avatar?: string | null, isOnline: boolean, lastSeenAt?: any | null } };

export const RegisterDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class RegisterGQL extends Apollo.Mutation<RegisterMutation, RegisterMutationVariables> {
    document = RegisterDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const LoginDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class LoginGQL extends Apollo.Mutation<LoginMutation, LoginMutationVariables> {
    document = LoginDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const CreateConversationDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class CreateConversationGQL extends Apollo.Mutation<CreateConversationMutation, CreateConversationMutationVariables> {
    document = CreateConversationDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const JoinConversationDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class JoinConversationGQL extends Apollo.Mutation<JoinConversationMutation, JoinConversationMutationVariables> {
    document = JoinConversationDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const SendMessageDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class SendMessageGQL extends Apollo.Mutation<SendMessageMutation, SendMessageMutationVariables> {
    document = SendMessageDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const GetMeDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class GetMeGQL extends Apollo.Query<GetMeQuery, GetMeQueryVariables> {
    document = GetMeDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const GetUsersDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class GetUsersGQL extends Apollo.Query<GetUsersQuery, GetUsersQueryVariables> {
    document = GetUsersDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const GetUserDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class GetUserGQL extends Apollo.Query<GetUserQuery, GetUserQueryVariables> {
    document = GetUserDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const GetMyConversationsDocument = gql`
    query GetMyConversations {
  myConversations {
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

  @Injectable({
    providedIn: 'root'
  })
  export class GetMyConversationsGQL extends Apollo.Query<GetMyConversationsQuery, GetMyConversationsQueryVariables> {
    document = GetMyConversationsDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const GetConversationDocument = gql`
    query GetConversation($id: ID!) {
  conversation(id: $id) {
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

  @Injectable({
    providedIn: 'root'
  })
  export class GetConversationGQL extends Apollo.Query<GetConversationQuery, GetConversationQueryVariables> {
    document = GetConversationDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const MessageSentDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class MessageSentGQL extends Apollo.Subscription<MessageSentSubscription, MessageSentSubscriptionVariables> {
    document = MessageSentDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const UserOnlineDocument = gql`
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

  @Injectable({
    providedIn: 'root'
  })
  export class UserOnlineGQL extends Apollo.Subscription<UserOnlineSubscription, UserOnlineSubscriptionVariables> {
    document = UserOnlineDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }