/**
 * User information returned from the API.
 */
export interface User {
    id: string;
    gitHubUsername: string;
    email: string | null;
    avatarUrl: string | null;
    createdAt: string;
    updatedAt: string;
}

/**
 * Authentication response from /api/auth/me
 */
export interface AuthResponse {
    isAuthenticated: boolean;
    user: User | null;
}

/**
 * Auth context State.
 * Holds the current authenticated user and loading state.
 */
export interface AuthState {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: string | null;
}

/**
 * Auth context value exposed to consumers.
 */
export interface AuthContextValue extends AuthState {
    login: () => void;
    logout: () => Promise<void>;
    refreshUser: () => Promise<void>;
}