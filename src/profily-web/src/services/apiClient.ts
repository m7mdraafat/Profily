interface HealthResponse {
    status: string;
    timestamp: string;
}

export async function request<T>(url: string) : Promise<T> {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data: T = await response.json();
        return data;
    } catch (error) {
        console.error("API request failed:", error);
        throw error;
    }
}

export async function getHealth() : Promise<HealthResponse> {
    const data = await request<HealthResponse>('/api/health');
    return data;
}
