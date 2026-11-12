import { Suspense } from "react";
import { redirect } from "next/navigation";
import { ApiError } from "../lib/api";
import { loadDashboard, type DashboardData } from "../lib/dashboard";
import { logoutAction } from "./login/action";
import ChallengeCard from "../components/ChallengeCard";


export default function DashboardPage() {
    return (
        <main className="mx-auto w-full max-w-4xl space-y-8 p-8">
            <Suspense fallback={<p>Loading your dashboard...</p>}>
            <DashboardContent />
            </Suspense>
        </main>
    )
}

async function DashboardContent() {
    let data : DashboardData;
    try {
        data = await loadDashboard();
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) redirect("/logout");
        throw error;
    }

    return (
        <>
            <header className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold">Hi, {data.displayName}</h1>
                <form action={logoutAction}>
                    <button type="submit" className="text-sm text-blue-600 hover:underline">Logout</button>
                </form>
            </header>

            {/* TODO : "New challenge" button -> POST /challenges */}

            <section>
                <h2 className="mb-3 text-lg font-semibold">My Challenges</h2>
                {data.myChallenges.length === 0 ? (
                    <p className="text-sm text-gray-600">You have no joined any challenges yet.</p>) : (
                        <ul className="grid gap-3 sm:grid-cols-2">
                            {data.myChallenges.map((challenge) => (
                                <ChallengeCard key={challenge.id} challenge={challenge} />
                            ))}
                        </ul>
                )}
            </section>

            <section>
                <h2 className="mb-3 text-lg font-semibold">Discover</h2>
                {data.discover.length === 0 ? (
                    <p className="text-sm text-gray-600">No open public challenges right now.</p>
                ):(
                    <ul className="grid gap-3 sm:grid-cols-2">
                        {data.discover.map((challenge) =>(
                            <ChallengeCard key={challenge.id} challenge={challenge}>
                                {/* TODO (other branch): Join button -> POST /memberships */}
                            </ChallengeCard>
                        ))}
                    </ul>
                )}
            </section>
        </>

    )
}