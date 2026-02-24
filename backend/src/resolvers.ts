export const resolvers = {
  Query: {
    me: () => null,
    users: () => [],
    user: () => null,
    myConversations: () => [],
    conversation: () => null,
  },
  Mutation: {
    register: () => ({ token: '', user: {} }),
    login: () => ({ token: '', user: {} }),
    createConversation: () => ({}),
    joinConversation: () => ({}),
    sendMessage: () => ({}),
  },
  Subscription: {
    messageSent: () => ({}),
    userOnline: () => ({}),
  },
};