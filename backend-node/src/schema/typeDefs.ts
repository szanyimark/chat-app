export const typeDefs = `#graphql
  type User {
    id: ID!
    email: String!
    username: String!
    avatar: String
    createdAt: String!
  }

  type Conversation {
    id: ID!
    type: ConversationType!
    name: String
    members: [User!]!
    messages: [Message!]!
    createdAt: String!
  }

  type Message {
    id: ID!
    conversation: Conversation!
    sender: User!
    content: String!
    createdAt: String!
  }

  type AuthPayload {
    token: String!
    user: User!
  }

  enum ConversationType {
    PRIVATE
    GROUP
  }

  input RegisterInput {
    email: String!
    username: String!
    password: String!
  }

  input LoginInput {
    email: String!
    password: String!
  }

  input CreateConversationInput {
    type: ConversationType!
    name: String
    memberIds: [ID!]!
  }

  input SendMessageInput {
    conversationId: ID!
    content: String!
  }

  type Query {
    me: User
    users: [User!]!
    user(id: ID!): User
    myConversations: [Conversation!]!
    conversation(id: ID!): Conversation
  }

  type Mutation {
    register(input: RegisterInput!): AuthPayload!
    login(input: LoginInput!): AuthPayload!
    createConversation(input: CreateConversationInput!): Conversation!
    joinConversation(id: ID!): Conversation!
    sendMessage(input: SendMessageInput!): Message!
  }

  type Subscription {
    messageSent(conversationId: ID!): Message!
    userOnline(userId: ID!): User!
  }
`;