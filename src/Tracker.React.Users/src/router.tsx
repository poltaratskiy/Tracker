import { createBrowserRouter } from "react-router-dom";
import App from './App';

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />/*,
    children: [
      { index: true, element: <Home /> },
      { path: "about", element: <About /> },
    ],*/
  },
  { path: '/callback', element: <OidcCallback /> },
]);