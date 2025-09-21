import { useAuth } from 'react-oidc-context';

export function AuthButtons() {
  const auth = useAuth();

  if (auth.isLoading) return null;

  return (
    <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
      {auth.isAuthenticated ? (
        <>
          <span>
            Hi, {auth.user?.profile?.preferred_username || auth.user?.profile?.name || auth.user?.profile?.email}
          </span>
          <button onClick={() => auth.signoutRedirect()}>Logout</button>
        </>
      ) : (
        <button onClick={() => {
          const state = location.pathname + location.search + location.hash;
          auth.signinRedirect({ state })
        }}>Login</button>
      )}
    </div>
  );
}