"use client";

import { useActionState, useState } from "react";
import Link from "next/link";
import FormField from "./FormField";
import { loginAction } from "../app/login/action";

export default function LoginForm() {
    const [state, formAction, pending] = useActionState(loginAction, undefined);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    return(
        <form action={formAction} className="w-full max-w-sm space-y-4">
            <h1 className="text-2xl font-bold">Login</h1>
            {state?.error && (<p role="alert" className="rounded-md bg-red-100 px-3 py-2 text-sm text-red-700">{state.error}</p>)}
            <FormField label="Email" name="email" type="email" value={email} onChange={setEmail}/>
            <FormField label="Password" name="password" type="password" autoComplete="current-password" value={password} onChange={setPassword}/>
            <button type="submit" disabled={pending} className="w-full rounded-md bg-blue-600 px-3 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-50">
                {pending ? "Logging in..." : "Login"}
            </button>
            <p className="text-center text-sm">No account yet?{" "}
                <Link href="/register" className="text-blue-600 hover:underline">
                    Register
                </Link></p>
        </form>
    )

}