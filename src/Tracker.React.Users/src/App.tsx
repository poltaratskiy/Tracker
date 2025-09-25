import AuthGate from './auth/AuthGate';
import { Outlet } from 'react-router';
import Navigation from './components/Navigation';


export default function App() {

  return (
    <AuthGate>
      <Navigation />
      <main>
        <Outlet />
      </main>
    </AuthGate>
  );
}
