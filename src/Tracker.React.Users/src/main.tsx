import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from 'react-oidc-context';
import { router } from './router';
import './index.css';
import { oidcConfig } from './auth/oidcConfig';
import { onSignInCallback } from './auth/onSignInCallback.ts';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider {...oidcConfig} onSigninCallback={onSignInCallback}>
      <RouterProvider router={router} />
    </AuthProvider>
  </StrictMode>,
)
