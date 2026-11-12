"use client";
import { useActionState, useEffect, useRef } from "react";
import Link from "next/link";
import { createChallengeAction } from "../app/challenges/actions";

const input = "w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600";

export default function NewChallengeForm() {
    const [state, formAction, pending] = useActionState(createChallengeAction, undefined);
    const startDate = useRef<HTMLInputElement>(null);

    // Default to the visitor's local date; computing it during render would break prerendering.
    useEffect(() => {
        if (startDate.current && !startDate.current.value) {
            startDate.current.value = new Date().toLocaleDateString("en-CA");
        }
    }, []);

    return (
        <form action={formAction} className="w-full max-w-md space-y-4">
            <h1 className="text-2xl font-semibold">New challenge</h1>

            {state?.error && (
                <div role="alert" className="rounded-md bg-red-100 px-3 py-2 text-sm text-red-700">
                    <p>{state.error}</p>
                    {state.fieldErrors?.length ? (
                        <ul className="mt-1 list-disc pl-5">
                            {state.fieldErrors.map((m) => <li key={m}>{m}</li>)}
                        </ul>
                    ) : null}
                </div>
            )}

            <div>
                <label htmlFor="title" className="mb-1 block text-sm font-medium">Title</label>
                <input id="title" name="title" required maxLength={100} defaultValue={state?.values?.title} className={input} />
            </div>
            <div>
                <label htmlFor="description" className="mb-1 block text-sm font-medium">Description</label>
                <textarea id="description" name="description" rows={3} defaultValue={state?.values?.description} className={input} />
            </div>
            <div><label htmlFor="visibility" className="mb-1 block text-sm font-medium">Visibility</label>
                <select id="visibility" name="visibility" defaultValue={state?.values?.visibility ?? "public"} className={input}>
                    <option value="public">Public</option>
                    <option value="private">Private</option>
                </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div>
                    <label htmlFor="startDate" className="mb-1 block text-sm font-medium">Start</label>
                    <input id="startDate" name="startDate" type="date" required ref={startDate} defaultValue={state?.values?.startDate} className={input} />
                </div>
                <div>
                    <label htmlFor="endDate" className="mb-1 block text-sm font-medium">End</label>
                    <input id="endDate" name="endDate" type="date" required defaultValue={state?.values?.endDate} className={input} />
                </div>
            </div>

            <button type="submit" disabled={pending} className="w-full rounded-md bg-blue-600 px-3 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-50">
                {pending ? "Creating..." : "Create challenge"}
            </button>
            <p className="text-center text-sm"><Link href="/" className="text-blue-600 hover:underline">Cancel</Link></p>
        </form>
    );
}