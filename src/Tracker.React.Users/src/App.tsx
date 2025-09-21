import { AuthButtons } from './components/AuthButtons';
import { ProtectedRoute } from './components/ProtectedRoute';
import AuthGate from './auth/authGate';
import { Outlet } from 'react-router';

function Secret() {
  return <div style={{ marginTop: 8 }}>🔒 Protected content</div>;
}

export default function App() {
  return (
    <div style={{ padding: 16 }}>
      <h1>Vite + React + react-oidc-context</h1>
      <AuthGate>
        <header style={{ padding: 12, borderBottom: "1px solid #eee" }}>
          {/* logo/navigation */}
          <AuthButtons />
        </header>
        <main>
          <Outlet />
        </main>

      </AuthGate>

      <AuthButtons />
      <hr />
      <ProtectedRoute>
        <Secret />
      </ProtectedRoute>
    </div>
  );
}



/*import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  )
}

export default App*/
