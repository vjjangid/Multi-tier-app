export interface User {
  id: string;
  username: string;
  email: string;
  fullName: string;
  avatar?: string;
  createdAt: Date;
  isGuest: boolean;
}

export interface LoginCredentials {
  username: string;
  password: string;
}

export interface SignupData {
  username: string;
  email: string;
  fullName: string;
  password: string;
  confirmPassword: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}