import jwt from 'jsonwebtoken';
import { GraphQLError } from 'graphql';
import { prisma } from './db';

const JWT_SECRET = process.env.JWT_SECRET || 'your-super-secret-jwt-key-change-in-production';

export interface Context {
  token?: string;
  user?: {
    id: string;
    email: string;
    username: string;
  };
}

export interface JwtPayload {
  userId: string;
  email: string;
  username: string;
}

export const generateToken = (userId: string, email: string, username: string): string => {
  return jwt.sign({ userId, email, username }, JWT_SECRET, { expiresIn: '7d' });
};

export const verifyToken = (token: string): JwtPayload => {
  try {
    return jwt.verify(token, JWT_SECRET) as JwtPayload;
  } catch (error) {
    throw new GraphQLError('Invalid or expired token', {
      extensions: { code: 'UNAUTHENTICATED' },
    });
  }
};

export const getUserFromToken = async (token?: string) => {
  if (!token) return null;

  try {
    const decoded = verifyToken(token.replace('Bearer ', ''));
    const user = await prisma.user.findUnique({
      where: { id: decoded.userId },
      select: {
        id: true,
        email: true,
        username: true,
        avatar: true,
        createdAt: true,
      },
    });
    return user;
  } catch {
    return null;
  }
};

export const authenticate = async (context: Context) => {
  if (!context.token) {
    throw new GraphQLError('Not authenticated', {
      extensions: { code: 'UNAUTHENTICATED' },
    });
  }

  const user = await getUserFromToken(context.token);
  if (!user) {
    throw new GraphQLError('Not authenticated', {
      extensions: { code: 'UNAUTHENTICATED' },
    });
  }

  return user;
};