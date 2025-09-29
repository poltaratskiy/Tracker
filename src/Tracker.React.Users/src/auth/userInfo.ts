import { useAuth } from "react-oidc-context";
import { useEffect, useState } from "react";

export type UserInfo = {
  fullName?: string;
  userName?: string;
  roles?: string[];
};

function parseJwt<T = any>(token?: string): T | null {
  if (!token) return null;
  try {
    const base64Url = token.split(".")[1];
    const binary  = atob(base64Url.replace(/-/g, "+").replace(/_/g, "/"));
    
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);

    // UTF-8 -> string
    const json = new TextDecoder("utf-8").decode(bytes);

    return JSON.parse(json);
  } catch {
    return null;
  }
}

function readFromStorage(): UserInfo {
  try {
    return JSON.parse(sessionStorage.getItem("userInfo") ?? "{}");
  } catch {
    return {};
  }
}

export function useUserInfo(): UserInfo {
  const auth = useAuth();

  // 1) Initial state from storage (to show something after presses F5)
  const [userInfo, setUserInfo] = useState<UserInfo>(() => readFromStorage());

  useEffect(() => {
    // 2) After access_token appeared — parsing and refreshing token and storage
    const token = auth.user?.access_token;
    if (!token) return;

    const payload = parseJwt<any>(token);
    if (!payload) return;

    const next: UserInfo = {
      fullName: payload.fullName,
      userName: payload.userName,
      roles: payload.roles,
    };

    setUserInfo(next);
    sessionStorage.setItem("userInfo", JSON.stringify(next));
  }, [auth.user?.access_token]);

  return userInfo;
}