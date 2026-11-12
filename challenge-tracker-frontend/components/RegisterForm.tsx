"use client";
import { useActionState, useState } from "react";
import Link from "next/link";
import FormField from "./FormField";
import { registerAction } from "../app/register/action";


export default function RegisterForm() {
    const [state, formAction, pending] = useActionState(registerAction, undefined);
    const [displayName, setDisplayName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    return (
        <form action={formAction} className="w-full max-w-sm space-y-4">
            <h1 className="text-2xl font-semibold">Create Account</h1>

            {state?.error && (
                <div role="alert" className="rounded-md bg-red-100 px-3 py-2 text-sm text-red-700">
                    <p>{state.error}</p>
                    {state.fieldErrors && state.fieldErrors.length > 0 && (
                        <ul className="mt-1 list-disc pl-5">
                            {state.fieldErrors.map((message) => (
                                <li key={message}>{message}</li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
            <FormField
                label="Name"
                name="displayName"
                autoComplete="name"
                minLength={2}
                value={displayName}
                onChange={setDisplayName}
            />
            <FormField
                label="Email"
                name="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={setEmail}
            />
            <FormField
                label="Password"
                type="password"
                name="password"
                autoComplete="new-password"
                minLength={8}
                value={password}
                onChange={setPassword}
            />

            <button type="submit" disabled={pending} className="w-full rounded-md bg-blue-600 px-3 py-2 font-medium text-white hover:bg-blue-700 display:opacity-50">
                {pending ? "Creating account..." : "Register"}
            </button>

            <p className="text-center text-sm">
                Already have an account?{" "}
                <Link href="/login" className="text-blue-600 hover:underline">
                    Log in
                </Link>
            </p>

        </form>
    )

}