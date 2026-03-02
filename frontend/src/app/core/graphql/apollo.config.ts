import { ApolloClient, InMemoryCache, ApolloLink } from '@apollo/client/core';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { createClient } from 'graphql-ws';
import { HttpLink } from '@apollo/client/link/http';
import { SetContextLink } from '@apollo/client/link/context';

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
    url: 'ws://localhost:5000/graphql',
    connectionParams: () => {
      const token = localStorage.getItem('auth_token');
      return {
        authorization: token ? `Bearer ${token}` : '',
      };
    },
  })
);

// Custom link that routes subscriptions to WebSocket and everything else to HTTP
const httpOrWsLink = new ApolloLink((operation, forward) => {
  const definition = operation.query.definitions[0] as { operation?: string };
  if (definition.operation === 'subscription') {
    return wsLink.request(operation);
  }
  return authLink.concat(httpLink).request(operation, forward);
});

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