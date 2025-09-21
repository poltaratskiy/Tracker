import { Navigate } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import type { JSX } from 'react';

export function ProtectedRoute({ children }: { children: JSX.Element }) {
  const auth = useAuth();

  if (auth.isLoading) return <p>Loading…</p>;
  if (!auth.isAuthenticated) return <Navigate to="/" replace />;

  return children;
}