import { Loader2 } from "lucide-react";
import { useAuth } from "../../contexts";
import { Navigate } from "react-router-dom";

interface ProtectedRouteProps {
    children: React.ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
    // Use useAuth() to get user state
    const { isAuthenticated, isLoading } = useAuth();

    return (
        <>
            {isLoading ? (
                <div className="flex items-center justify-center min-h-[50vh]">
                    <Loader2 className="animate-spin text-slate-400" size={32} />
                </div>
            ) : !isAuthenticated ? (
                <Navigate to="/" replace />
            ) : (
                {children}
            )}
        </>
    )
}