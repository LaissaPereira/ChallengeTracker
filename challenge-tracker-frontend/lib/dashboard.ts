import "server-only";
import { getMe, getChallenges, type ChallengeListItem } from "./api";

// Functions to fetch dashboard-related data from the API

export type DashboardData = {
    displayName: string;
    myChallenges: ChallengeListItem[];
    discover: ChallengeListItem[];
};

export async function loadDashboard() : Promise<DashboardData> {

    const [me, challenges] = await Promise.all([getMe(), getChallenges()]);

    const isMine = (challenge: ChallengeListItem) =>
        challenge.ownerId === me.id || challenge.currentUserMembershipStatus === "active";

    return {
        displayName: me.displayName,
        myChallenges: challenges.filter(isMine),
        discover: challenges.filter((challenge) => !isMine(challenge) && challenge.visibility === "public" && challenge.status === "open",),
    }

}
