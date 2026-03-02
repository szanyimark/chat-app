export interface User {
  id: string;
  email: string;
  username: string;
  avatar: string | null;
  createdAt: string;
  isOnline: boolean;
  lastSeenAt: string | null;
}

export interface AuthPayload {
  token: string;
  user: User;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface RegisterInput {
  email: string;
  username: string;
  password: string;
}