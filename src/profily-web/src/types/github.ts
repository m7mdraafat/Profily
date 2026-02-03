/**
 * GitHub repository data from API.
 */
export interface GitHubRepository {
    id: number;
    name: string;
    fullName: string;
    description: string | null;
    htmlUrl: string;
    homePage: string | null;
    language: string | null;
    starsCount: number;
    forksCount: number;
    isFork: boolean;
    isArchived: boolean;
    isPrivate: boolean;
    languages: string[] | null;
    topics: string[] | null;
    createdAt: string;
    updatedAt: string;
    pushedAt: string | null;
}

/**
 * Language statistics for a repository or user.
 */
export interface LanguageStat {
    name: string;
    bytes: number;
    percentage: number;
    color: string | null;
}

/**
 * Aggregated GitHub statistics for a user.
 */
export interface GitHubStats {
    username: string;
    publicReposCount: number;
    totalStars: number;
    totalForks: number;
    followers: number;
    following: number;
    topLanguages: LanguageStat[];
    fetchedAt: string;
}
