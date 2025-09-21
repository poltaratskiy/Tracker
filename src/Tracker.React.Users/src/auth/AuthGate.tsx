import { useEffect } from "react";
import type { PropsWithChildren } from "react";
import { useAuth } from "react-oidc-context";
import { useLocation } from "react-router-dom";

export default function AuthGate({ children }: PropsWithChildren) {
  const auth = useAuth();
  const location = useLocation();

  useEffect(() => {
    if (auth.isLoading) return;
    // avioding double redirects: activeNavigator = 'signinRedirect' when redirecting is already in process
    if (!auth.isAuthenticated && !auth.activeNavigator && !auth.error) {
      const state = location.pathname + location.search + location.hash; // path to return after login
      auth.signinRedirect({ state }).catch(() => {/* no-op */});
    }
  }, [auth.isLoading, auth.isAuthenticated, auth.activeNavigator, auth.error, location, auth]);

  if (auth.isLoading || auth.activeNavigator || !auth.isAuthenticated)
    return; // can show spinner if want

  return <>{children}</>;
}