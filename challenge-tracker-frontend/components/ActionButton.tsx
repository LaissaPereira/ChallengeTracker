"use client";
import { useActionState } from "react";
import type { ActionState } from "../app/challenges/actions";

type Props = {
    action: (prev: ActionState, formData: FormData) => Promise<ActionState>;
    label: string;
    pendingLabel: string;
    tone?: "primary" | "neutral" | "danger";
}

const tones = {
    primary: "bg-green-700 text-white hover:bg-green-800",
    neutral: "border border-gray-300 hover:bg-gray-100",
    danger: "border border-red-300 text-red-700 hover:bg-red-50"
} as const;


export default function ActionButton({ action, label, pendingLabel, tone = "primary" }: Props) {
    const [state, formAction, pending] = useActionState(action, undefined);

    return (
        <form action={formAction} className="inline-block">
            <button
            type="submit"
            className={`rounded-md px-3 py-1.5 text-sm font-medium disabled:opacity-50 ${tones[tone]}`}
            disabled={pending}
            >
                {pending ? pendingLabel : label}
            </button>
            {state?.error && <p role="alert" className="mt-1 text-xs text-red-700">{state.error}</p>}

        </form>
    );
}