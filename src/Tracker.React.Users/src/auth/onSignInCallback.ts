import type { User } from "oidc-client-ts";

export function onSignInCallback(user?: User) {
  console.log("Callback after login, user: " + user?.profile.name)
}