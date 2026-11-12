"use server";
import { redirect } from "next/navigation";
import { login, ApiError } from "../../lib/api";
import { saveSession, clearSession } from "../../lib/session";

export async function loginAction(_prev: { error?: string} | undefined, formData: FormData){
    try {
        const {token, expiresAt } = await login(
            String(formData.get("email")),
            String(formData.get("password")),
        );
        await saveSession(token, expiresAt);
    } catch(error) {
        if(error instanceof ApiError) return { error: error.message };
        throw error;
    }
    redirect("/")
}

export async function logoutAction() {
    await clearSession();
    redirect("/login");
}