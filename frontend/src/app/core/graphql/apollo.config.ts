import { ApolloClient, InMemoryCache, split } from '@apollo/client/core';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { createClient } from 'graphql-ws';
import { HttpLink } from '@apollo/client/link/http';
import { SetContextLink } from '@apollo/client/link/context';
import { getMainDefinition } from '@apollo/client/utilities';

const httpLink = new HttpLink({
  uri: 'http://localhost:5000/graphql',
});

const authLink = new SetContextLink((prevContext, operation) => {
  const token = localStorage.getItem('auth_token');
  const headers = prevContext['headers'] as Record<string, string> | undefined;
  return {
    ...prevContext,
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : '',
    },
  };
});

const wsLink = new GraphQLWsLink(
  createClient({
    url: () => {
      const token = localStorage.getItem('auth_token');
      const encodedToken = token ? encodeURIComponent(token) : '';
      return encodedToken
        ? `ws://localhost:5000/graphql?access_token=${encodedToken}`
        : 'ws://localhost:5000/graphql';
    },
    connectionParams: () => {
      const token = localStorage.getItem('auth_token');
      return {
        authorization: token ? `Bearer ${token}` : '',
      };
    },
    on: {
      connected: () => {},
      error: (error) => {
        console.error('[WS] WebSocket error:', error);
      },
      closed: () => {},
    },
    shouldRetry: () => true,
  })
);

const authedHttpLink = authLink.concat(httpLink);

// Route subscriptions to WebSocket and queries/mutations to HTTP.
const httpOrWsLink = split(
  ({ query }) => {
    const definition = getMainDefinition(query);
    return definition.kind === 'OperationDefinition' && definition.operation === 'subscription';
  },
  wsLink,
  authedHttpLink
);

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function createApollo(): any {
  return new ApolloClient({
    link: httpOrWsLink,
    cache: new InMemoryCache(),
    defaultOptions: {
      watchQuery: {
        fetchPolicy: 'cache-and-network',
      },
    },
  });
}