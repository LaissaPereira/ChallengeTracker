import "server-only";
import { cookies } from "next/headers";

const COOKIE = "auth_token";

export async function saveSession(token: string, expiresAt: string) {
    const store = await cookies();
    store.set(COOKIE, token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        expires: new Date(expiresAt)
    })
}

export async function getToken() {
    return (await cookies()).get(COOKIE)?.value;
}

export async function clearSession() {
    (await cookies()).delete(COOKIE);
}