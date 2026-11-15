"use server"
import { redirect } from "next/navigation";
import { register, login, ApiError } from "../../lib/api";
import { saveSession } from "../../lib/session";


export type RegisterState = { error?: string; fieldErrors?: string[] } | undefined;

export async function registerAction(_prev: RegisterState, formData: FormData): Promise<RegisterState>{
    const displayName = String(formData.get("displayName"));
    const email = String(formData.get("email"));
    const password = String(formData.get("password"));

    try {
        await register(displayName, email, password);
        const { token, expiresAt } = await login(email, password);
        await saveSession(token, expiresAt);
    } catch(error) {
        if (error instanceof ApiError) {
            return { error: error.message, fieldErrors: error.fieldErrors};
        }
        throw error;
    }
    redirect("/")
}
