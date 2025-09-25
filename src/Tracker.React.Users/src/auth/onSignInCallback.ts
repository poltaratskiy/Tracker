import type { User } from "oidc-client-ts";

export function onSignInCallback(user?: User) {
  // remove query parameters
  const cleanUrl = window.location.origin + window.location.pathname + window.location.hash;
  window.history.replaceState({}, "", cleanUrl);

  const target = (user?.state as string | undefined) || "/";
  window.history.replaceState({}, document.title, target);
}