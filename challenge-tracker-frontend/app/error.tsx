"use client";

export default function Error({ reset} : { error: Error, reset: () => void}) {
    return (
        <main className="p-6">
            <p className="mb-3">Something went wrong while loading the page..</p>
            <button onClick={reset} className="text-blue-600 hover:underline">Try again</button>
        </main>
    )
}