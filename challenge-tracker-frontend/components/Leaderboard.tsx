import type { LeaderboardEntry } from "../lib/api";

type Props = {
    entries: LeaderboardEntry[];
    currentUserId: string;
};

export default function Leaderboard({ entries, currentUserId }: Props) {
    if (entries.length === 0) {
        return <p className="text-sm text-gray-600">No active members yet.</p>;
    }

    return (
        <ol className="space-y-2">
            {entries.map((entry) => (
                <li
                    key={entry.userId}
                    className={`flex items-center justify-between rounded-lg border p-3 text-sm ${entry.userId === currentUserId ? "border-(--accent)" : "border-(--border)"}`}
                >
                    <span>
                        <span className="mr-3 inline-block w-6 text-gray-500">#{entry.rank}</span>
                        {entry.displayName}
                        {entry.userId === currentUserId && " (you)"}
                    </span>
                    <strong>{entry.totalProgress}</strong>
                </li>
            ))}
        </ol>
    );
}
