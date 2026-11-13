import type { ReactNode } from "react";
import Link from "next/link";
import type { ChallengeListItem } from "../lib/api";

export const statusStyle = {
    open: "bg-green-100 text-green-800",
    running: "bg-blue-100 text-blue-800",
    completed: "bg-gray-100 text-gray-800",
} as const;


export default function ChallengeCard({ challenge, children} : { challenge: ChallengeListItem; children?: ReactNode}){

    return(
        <li className="rounded-lg border border-(--border) p-4">
            <div className="flex items-start justify-between gap-3">
                <h3 className="font-semibold">
                    <Link href={`/challenges/${challenge.id}`} className="hover:underline">{challenge.title}</Link>
                </h3>
                <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusStyle[challenge.status]}`} >{challenge.status}</span>
            </div>

            {challenge.description && (
                <p className="mt-1 text-sm text-gray-600">{challenge.description}</p>
            )}
            <p className="mt-2 text-xs text-gray-500">
                {challenge.startDate} → {challenge.endDate} · by {challenge.ownerDisplayName} · {challenge.activeMemberCount} {challenge.activeMemberCount === 1 ? "member" : "members"}
            </p>
            {challenge.currentUserMembershipStatus === "pending" && (
                <p className="mt-2 text-xs font-medium text-amber-700">Request pending</p>
            )}
            {children}
        </li>
    );
}