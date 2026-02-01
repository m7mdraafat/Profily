import { useState, useEffect } from 'react'
import { getHealth } from '../services/apiClient';

export function HomePage() {
    const [status, setStatus] = useState<string>('loading');
    const [data, setData] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    async function checkHealth() : Promise<void> {
        setStatus('loading');
        setError(null);

        try {
            const result =  await getHealth();

            setStatus("Ok");
            setData(JSON.stringify(result, null, 2));
        } catch (err: unknown) {
            setStatus('Error');
            setError(err instanceof Error ? err.message : String(err));
        }
    }

    useEffect(() => {
        checkHealth();
    }, []);

    return (
        <>
            <h1>Home Page</h1>
            {status === "loading" ? (
                <p>Loading...</p>
            ) : status === "Ok" ? (
                <pre>{data}</pre>
            ) : (
                <p style={{color: "red"}}>Error: {error}</p>
            )}

            <button onClick={checkHealth}>Refresh</button>
        </>
    )
}
