import { useAuth } from 'react-oidc-context';
import { useUserInfo } from '../auth/userInfo';

export default function Navigation() {
    const auth = useAuth();
    const userInfo = useUserInfo();
    const roles = userInfo.roles ?? [];

    return (
        <header>
            <div className='header-container'>
                <div className='header-service-name'>
                    <h1>{import.meta.env.VITE_SERVICE_NAME}</h1>
                </div>
                <div className='header-right'>
                    <div className="header-links">
                        {roles.includes("Accountant") ? <a href={import.meta.env.VITE_LINKS_REPORTINGSERVICE!}>Reports</a> : null}
                        {roles.includes("Manager") || roles.includes("Admin") || roles.includes("User") ? <a href={import.meta.env.VITE_LINKS_TASKTRACKER!}>Tasks tracker</a> : null}
                        <a href={import.meta.env.VITE_LINKS_USERSSERVICE!}>Users</a>
                    </div>
                    <div className='header-username'>
                        Hi, {userInfo.fullName}
                    </div>
                    <button onClick={() => auth.signoutRedirect()}>Logout</button>
                </div>
            </div>
        </header>
    );
}