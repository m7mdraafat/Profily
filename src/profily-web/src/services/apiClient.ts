import type { AuthResponse } from "../types/auth";

const API_BASE = '/api';


/**
 * Generic request function with credential for cookie auth.
 */
export async function request<T>(
    url: string,
    options: RequestInit = {}
): Promise<T> {
    const response = await fetch(`${API_BASE}${url}`, {
        ...options,
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
            ...options.headers
        }
    });

    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Request failed' }));
        throw new ApiError(response.status, error.message || `HTTP ${response.status}`);
    }

    return response.json();
}

/**
 * Custom error class for API errors
 */
export class ApiError extends Error {
    public status: number;

    constructor(
        status: number,
        message: string
    ) {
        super(message);
        this.status = status;
        this.name = 'ApiError';
    }
}

// ========== Health Check ==========
interface HealthResponse {
  status: string;
}
export async function getHealth(): Promise<HealthResponse> {
  return request<HealthResponse>('/health');
}

// ========== Auth ==========
/**
 * Gets the current authenticated user.
 * Returns null if not authenticated (401).
 */
export async function getMe(): Promise<AuthResponse> {
    try {
        return await request<AuthResponse>('/auth/me');
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            return { isAuthenticated: false, user: null };
        }
        throw error;
    }  
}

/**
 * Logs out the current user.
 */
export async function logout(): Promise<void> {
    await request<{ message: string }>('/auth/logout', {
        method: 'POST'
    });
}

/**
 * Returns the URL to initiate GitHub OAuth login.
 */
export function getLoginUrl(returnUrl?: string) : string {
    const params = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    return `${API_BASE}/auth/github/${params}`;
}