import { Suspense } from "react";
import { redirect } from "next/navigation";
import Link from "next/link";
import ActionButton from "../components/ActionButton";
import { joinChallengeAction } from "./challenges/actions";
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
            <header className="flex flex-wrap items-center justify-between gap-3">
                <h1 className="text-2xl font-semibold">Hi, {data.displayName}</h1>
                <div className="flex items-center gap-3">
                    <Link href="/challenges/new" className="inline-block rounded-full bg-green-700 px-4 py-2 text-sm font-medium text-white hover:bg-green-800">
                        New challenge
                    </Link>
                    <form action={logoutAction}>
                        <button type="submit" className="text-sm">Logout</button>
                    </form>
                </div>
            </header>

            <section>
                <h2 className="mb-3 text-lg font-semibold">My Challenges</h2>
                {data.myChallenges.length === 0 ? (
                    <p className="text-sm text-gray-600">You haven&apos;t joined any challenges yet.</p>) : (
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
                                {challenge.currentUserMembershipStatus === null && (
                                    <div className="mt-3">
                                        <ActionButton action={joinChallengeAction.bind(null, challenge.id)} label="Join" pendingLabel="Joining..." />
                                    </div>
                                )}
                            </ChallengeCard>
                        ))}
                    </ul>
                )}
            </section>
        </>

    )
}