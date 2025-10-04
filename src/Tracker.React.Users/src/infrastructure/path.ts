export const ORIGIN = window.location.origin;

export const API = (path: string) =>
{
    const baseUrl = new URL('/api', ORIGIN);
    return new URL(path, baseUrl);
}