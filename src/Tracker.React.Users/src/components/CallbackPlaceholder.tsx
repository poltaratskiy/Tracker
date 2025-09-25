import { useAuth } from "react-oidc-context";
import { useEffect } from "react";
import { router } from "../router";

export function CallbackPlaceholder() {
  const { user, isAuthenticated } = useAuth();

  // If we are already logged show the loading screen
  useEffect(() => {
    if (!isAuthenticated) return;
    const target = (user?.state as string | undefined) || "/";
    router.navigate(target, { replace: true });
    window.scrollTo(0, 0);
  }, [isAuthenticated, user]);

  // not nessesary but why not - can be changed by spin
  return (
    <div
        role="status"
        aria-live="polite"
        style={{
          position: "absolute",
          inset: 0,
          zIndex: 9999,
          display: "grid",
          placeItems: "center",
          background: "#fff"
        }}
      >
        Logging...
      </div>
  );
}