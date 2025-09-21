import type { User } from "oidc-client-ts";

export function onSignInCallback(user?: User) {
  //Todo: remove query parameters
  const target = (user?.state as string | undefined) || "/";
  window.history.replaceState({}, document.title, target);
}