import {
    createContext,
    useContext,
    useState,
    useEffect,
    useCallback,
    type ReactNode,
} from 'react';
import { getMe, logout as apiLogout, getLoginUrl } from '../services/apiClient';
import type { User, AuthContextValue } from '../types/auth';

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
    children: ReactNode;
}

export function AuthProvider({children} : AuthProviderProps) {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const isAuthenticated = user !== null;

    /**
     * Fetches current user from backend
     */
    const refreshUser = useCallback(async () => {
        try {
            setIsLoading(true);
            setError(null);
            const response = await getMe();
            setUser(response.user);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to fetch user');
            setUser(null);
        } finally {
            setIsLoading(false);
        }
    }, []);

    /**
     * Redirects to GitHub OAuth login.
     */
    const login = useCallback(() => {
        const currentPath = window.location.pathname;
        window.location.href = getLoginUrl(currentPath);
    }, []);

    /**
     * Logs out and clears user state.
     */
    const logout = useCallback(async () => {
        try {
            setIsLoading(true);
            await apiLogout();
            setUser(null);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Logout failed');
        } finally {
            setIsLoading(false);
        }
    }, []);

    // Check auth status on mount
    useEffect(() => {
        refreshUser();
    }, [refreshUser]);

    const value: AuthContextValue = {
        user,
        isAuthenticated,
        isLoading,
        error,
        login,
        logout,
        refreshUser
    };

    return <AuthContext.Provider value={value}> {children}</AuthContext.Provider>;
}

/**
 * Hook to access auth context.
 * Must be used within AuthProvider.
 */
export function useAuth(): AuthContextValue {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }

    return context;
}