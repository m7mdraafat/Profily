import { useState, useEffect, useCallback } from "react";
import { getGitHubStats, getRepositories, ApiError } from "../services/apiClient";
import type { GitHubRepository, GitHubStats } from "../types/github";

interface UseGitHubRepositoriesResult {
    repositories: GitHubRepository[];
    isLoading: boolean;
    error: string | null;
    refetch: () => Promise<void>;
}

interface UseGitHubStatsResult {
    stats: GitHubStats | null;
    isLoading: boolean;
    error: string | null;
    refetch: () => Promise<void>;
}

/**
 * Hook to fetch the authenticated user's GitHub repositories.
 */
export function useGitHubRepositories(): UseGitHubRepositoriesResult {
    const [repositories, setRepositories] = useState<GitHubRepository[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const fetchRepositories = useCallback(async () => {
        setIsLoading(true);
        setError(null);

        try {
            const data = await getRepositories();
            setRepositories(data);
        } catch (err) {
            if (err instanceof ApiError) {
                setError(`Error ${err.status}: ${err.message}`);
            }
            else {
                setError('Failed to fetch repositories.');
            }
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchRepositories();
    }, [fetchRepositories]);

    return { repositories, isLoading, error, refetch: fetchRepositories };
}

/**
 * Hook to fetch the authenticated user's aggregated GitHub statistics.
 */
export function useGitHubStats(): UseGitHubStatsResult {
    const [stats, setStats] = useState<GitHubStats | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const fetchStats = useCallback(async () => {
        setIsLoading(true);
        setError(null);

         try {
            const data = await getGitHubStats();
            setStats(data);
         } catch (err) {
            if (err instanceof ApiError) {
                setError(`Error ${err.status}: ${err.message}`);
            } else {
                setError('Failed to fetch GitHub statistics.');
            }
         } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchStats();
    }, [fetchStats]);

    return { stats, isLoading, error, refetch: fetchStats };
}