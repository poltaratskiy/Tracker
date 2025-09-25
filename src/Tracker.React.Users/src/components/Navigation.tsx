import { useAuth } from 'react-oidc-context';
import { useUserInfo } from '../auth/userInfo';

export default function Navigation() {
    const auth = useAuth();
    const userInfo = useUserInfo();
    const roles = userInfo.roles ?? [];

    return (
        <header>
            <div className="header-content">
                <div className="header-links">
                    {roles.includes("Accountant") ? <a href={import.meta.env.VITE_LINKS_REPORTINGSERVICE!}>Reports</a> : null}
                    {roles.includes("Manager") || roles.includes("Admin") || roles.includes("User") ? <a href={import.meta.env.VITE_LINKS_TASKTRACKER!}>Tasks tracker</a> : null}
                    <a href={import.meta.env.VITE_LINKS_USERSSERVICE!}>Users</a>
                </div>
                <span>
                Hi, {userInfo.fullName}
            </span>
            <button onClick={() => auth.signoutRedirect()}>Logout</button>
            </div>
        </header>
    );
}