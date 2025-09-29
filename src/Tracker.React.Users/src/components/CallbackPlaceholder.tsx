import { useAuth } from "react-oidc-context";
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

export function CallbackPlaceholder() {
  const { user, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  // clean query parameters
  
  // If we are already logged show the loading screen
  useEffect(() => {
    if (!isAuthenticated) return;
    const target = (user?.state as string | undefined) || "/";
    const u = new URL(target, window.location.origin);

    // remove parameters from query
    stripParamsFromUrl(u, ["iss"]);
    stripParamsFromUrl(u, ["sid"]);

    const clean = u.pathname + u.search + u.hash;

    // redirect to the address from state
    navigate(clean, { replace: true });
    window.scrollTo(0, 0);
  }, [isAuthenticated, user]);

  // not nessesary but why not - can be changed by spin
  return (
    <output
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
    </output>
  );
}

function stripParamsFromUrl(url: URL, keys: string[]) {

  // --- remove parameters from query (?a=1&b=2) ---
  const q = url.searchParams;
  for (const k of keys) q.delete(k);
  url.search = q.toString() ? `?${q.toString()}` : "";
}