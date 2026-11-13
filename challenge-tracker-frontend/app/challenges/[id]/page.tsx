import { Suspense } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ApiError, getChallenge, getMe } from "../../../lib/api";
import ActionButton from "../../../components/ActionButton";
import ProgressForm from "../../../components/ProgressForm";
import { statusStyle } from "../../../components/ChallengeCard";
import {
    approveMembershipAction, completeChallengeAction, deleteProgressAction,
    joinChallengeAction, leaveChallengeAction, startChallengeAction,
} from "../actions";

export default function ChallengePage({ params }: PageProps<"/challenges/[id]">) {
    return (
        <main className="mx-auto w-full max-w-4xl space-y-6 p-8">
            <Link href="/" className="text-sm">← Back to dashboard</Link>
            <Suspense fallback={<p>Loading challenge...</p>}>
                <ChallengeContent params={params} />
            </Suspense>
        </main>
    );
}

async function ChallengeContent({ params }: { params: PageProps<"/challenges/[id]">["params"] }) {
    const { id } = await params;
    let challenge, me;
    try {
        [challenge, me] = await Promise.all([getChallenge(id), getMe()]);
    } catch (error) {
        if (error instanceof ApiError) {
            if (error.status === 401) redirect("/logout");
            if (error.status === 404) redirect("/");
        }
        throw error;
    }

    const isOwner = challenge.ownerId === me.id;
    const { membershipId, membershipStatus, todayProgress } = challenge.currentUser;
    const isActive = membershipStatus === "active";

    return (
        <>
            <header className="space-y-1">
                <div className="flex items-start justify-between gap-3">
                    <h1 className="text-2xl font-semibold">{challenge.title}</h1>
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusStyle[challenge.status]}`}>{challenge.status}</span>
                </div>
                {challenge.description && <p className="text-sm text-gray-600">{challenge.description}</p>}
                <p className="text-xs text-gray-500">
                    {challenge.startDate} → {challenge.endDate} · by {challenge.ownerDisplayName} · {challenge.visibility}
                </p>
            </header>

            <div className="flex flex-wrap gap-2">
                {isOwner && challenge.status === "open" && (
                    <ActionButton action={startChallengeAction.bind(null, id)} label="Start challenge" pendingLabel="Starting..." />
                )}
                {isOwner && challenge.status === "running" && (
                    <ActionButton action={completeChallengeAction.bind(null, id)} label="Complete challenge" pendingLabel="Completing..." />
                )}
                {!isOwner && membershipId === null && challenge.status === "open" && (
                    <ActionButton action={joinChallengeAction.bind(null, id)} label="Join" pendingLabel="Joining..." />
                )}
                {!isOwner && membershipId !== null && (
                    <ActionButton action={leaveChallengeAction.bind(null, membershipId)} label={membershipStatus === "pending" ? "Cancel request" : "Leave"} pendingLabel="Leaving..." tone="danger" />
                )}
            </div>

            {isActive && (
                <section>
                    <h2 className="mb-3 text-lg font-semibold">Today</h2>
                    {todayProgress ? (
                        <div className="flex items-center justify-between gap-3 rounded-lg border border-(--border) p-4">
                            <p className="text-sm">
                                Logged <strong>{todayProgress.amount}</strong>
                                {todayProgress.note ? ` · ${todayProgress.note}` : ""}
                            </p>
                            <ActionButton action={deleteProgressAction.bind(null, todayProgress.id, id)} label="Delete" pendingLabel="Deleting..." tone="danger" />
                        </div>
                    ) : challenge.status === "completed" ? (
                        <p className="text-sm text-gray-600">This challenge is completed, so progress can no longer be logged.</p>
                    ) : (
                        <ProgressForm challengeId={id} />
                    )}
                </section>
            )}

            <section>
                <h2 className="mb-3 text-lg font-semibold">Members</h2>
                <ul className="space-y-2">
                    {challenge.members.map((member) => (
                        <li key={member.membershipId} className="flex items-center justify-between rounded-lg border border-(--border) p-3 text-sm">
                            <span>{member.displayName}{member.userId === challenge.ownerId && " (owner)"}</span>
                            {member.status === "pending" ? (
                                isOwner ? (
                                    <ActionButton action={approveMembershipAction.bind(null, member.membershipId, id)} label="Approve" pendingLabel="Approving..." />
                                ) : (
                                    <span className="text-xs text-amber-700">Pending</span>
                                )
                            ) : (
                                <span className="text-xs text-gray-500">Joined {member.joinedAt.slice(0, 10)}</span>
                            )}
                        </li>
                    ))}
                </ul>
            </section>
        </>
    );
}