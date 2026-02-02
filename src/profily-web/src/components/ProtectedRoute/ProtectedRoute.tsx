import { Navigate } from 'react-router-dom';
import { useEffect, useRef } from 'react';
import { useAuth, useToast, TOAST_TYPE } from '../../contexts';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { isAuthenticated, isLoading, login } = useAuth();
  const { showToast } = useToast();
  const hasShownToast = useRef(false);

  useEffect(() => {
    // Show toast only once when redirecting unauthenticated user
    if (!isLoading && !isAuthenticated && !hasShownToast.current) {
      hasShownToast.current = true;
      showToast(TOAST_TYPE.WARNING, 'Please sign in to access this feature', {
        label: 'Sign In',
        onClick: login,
      });
    }
  }, [isLoading, isAuthenticated, showToast, login]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
};
