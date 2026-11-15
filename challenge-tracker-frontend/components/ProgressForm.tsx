"use client";
import { useActionState } from "react";
import { logProgressAction } from "../app/challenges/actions";

const field = "w-full rounded-md border border-(--border) bg-transparent px-3 py-2 outline-none focus:border-(--accent) focus:ring-1 focus:ring-(--accent)";

export default function ProgressForm({ challengeId }: { challengeId: string }) {
    const [state, formAction, pending] = useActionState(logProgressAction.bind(null, challengeId), undefined);
    return (
        <form action={formAction} className="space-y-3">
            {state?.error && <p role="alert" className="rounded-md bg-red-100 px-3 py-2 text-sm text-red-700">{state.error}</p>}
            <input name="amount" type="number" step="any" min="0" required placeholder="Amount" aria-label="Amount" defaultValue={state?.values?.amount} className={field} />
            <input name="note" placeholder="Note (optional)" aria-label="Note" defaultValue={state?.values?.note} className={field} />
            <button type="submit" disabled={pending}
                className="rounded-md bg-green-700 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-800 disabled:opacity-50">
                {pending ? "Saving..." : "Log today's progress"}
            </button>
        </form>
    );
}