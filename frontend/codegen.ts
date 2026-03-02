import type { CodegenConfig } from '@graphql-codegen/cli';

const config: CodegenConfig = {
  schema: './src/app/core/graphql/schema.graphql',
  documents: 'src/app/core/graphql/operations/*.ts',
  generates: {
    'src/app/core/graphql/generated/graphql.ts': {
      plugins: [
        'typescript',
        'typescript-operations',
        'typescript-apollo-angular',
      ],
      config: {
        addTypename: true,
        strict: true,
      },
    },
  },
};

export default config;