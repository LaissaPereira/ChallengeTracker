import "server-only";
const API_URL = process.env.API_URL;
import { getToken } from "./session";

export type LoginResponse = {token: string; expiresAt: string};
export type User = {id: string; displayName: string; email: string};

export class ApiError extends Error {
    constructor(
        message: string,
        public status: number,
        public fieldErrors: string[] = [],
    ){
        super(message);
    }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    if(!API_URL){
        throw new ApiError("The API URL is not configured", 0);
    }
    let response: Response;
    try {
        response = await fetch(`${API_URL}${path}`,init);
    } catch {
        throw new ApiError("Cannot reach the server. Please try again later", 0);
    }
    if(response.ok){
        if(response.status ===204) return undefined as T;
        return (await response.json()) as T;
    }
    if(response.status === 401){
        throw new ApiError("Invalid email or password", 401);

    }
    if (response.status === 403) throw new ApiError("You are not allowed to do that", 403);
    if (response.status === 404) throw new ApiError("Not found", 404);
    if (response.status === 409) {
        const body = await response.json().catch(() => null);
        throw new ApiError(body?.detail ?? "Conflict", 409);
    }
    if(response.status == 429){
        throw new ApiError("Too many requests. Please try again later", 429);
    }
    const body = await response.json().catch(() => null);
    if(response.status === 400) {
        const messages = body?.errors ? (Object.values(body.errors as string[][]).flat()) : [];
        throw new ApiError( body?.detail ?? "Bad Request", 400, messages);
    }
    throw new ApiError(body?.detail ?? `Unexpected error: ${response.statusText}`, response.status);
}
const jsonPost = (body: unknown): RequestInit => ({
    method: "POST",
    headers: {
        "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
});

export const login = (email: string, password: string) =>
    request<LoginResponse>("/auth/login", jsonPost({email, password}));

export const register = (displayName: string, email: string, password: string) =>
    request<User>("/auth/register", jsonPost({displayName, email, password}));



export type ChallengeStatus = "open" | "running" | "completed";
export type ChallengeVisibility = "public" | "private";
export type MembershipStatus = "pending" | "active";

export type ChallengeListItem = {
    id: string;
    title: string;
    description: string | null;
    visibility: ChallengeVisibility;
    status: ChallengeStatus;
    startDate: string;
    endDate: string;
    ownerId: string;
    ownerDisplayName: string;
    activeMemberCount: number;
    currentUserMembershipStatus: MembershipStatus | null;
};

export type ChallengeMember = {
    membershipId: string;
    userId: string;
    displayName: string;
    status: MembershipStatus;
    joinedAt: string;
}

export type ChallengeDetail = {
    id: string;
    title: string;
    description: string | null;
    visibility: ChallengeVisibility;
    status: ChallengeStatus;
    startDate: string;
    endDate: string;
    createdAt: string;
    ownerId: string;
    ownerDisplayName: string;
    members: ChallengeMember[];
    currentUser: {
        membershipId: string | null;
        membershipStatus: MembershipStatus | null;
        todayProgress: {id: string; amount: number; note: string | null; loggedAt: string} | null;
    }
};
export type NewChallenge = {
    title: string;
    description?: string;
    visibility: ChallengeVisibility;
    startDate: string;
    endDate: string;

}

export type Membership = {
    id: string;
    userId: string;
    challengeId: string;
    status: MembershipStatus;
    joinedAt: string;
};

export type ProgressEntry = {
    id: string;
    userId: string;
    challengeId: string;
    amount: number;
    note: string | null;
    loggedAt: string;
    createdAt: string;
};


async function authGet<T>(path: string): Promise<T> {
    const token = await getToken();
    if(!token) throw new ApiError("Not authenticated", 401);
    return request<T>(path, {headers: {Authorization: `Bearer ${token}`},});
}
async function authSend<T>(path:string, method: "POST" | "PUT" | "DELETE", body?: unknown): Promise<T> {
    const token = await getToken();
    if(!token) throw new ApiError("Not authenticated", 401);
    return request<T>(path, {
        method,
        headers: {
            Authorization: `Bearer ${token}`,
            ...(body !== undefined && { "Content-Type": "application/json" })
        },
        body: body !== undefined ? JSON.stringify(body) : undefined
    });
}


export const getMe = () => authGet<User>("/auth/me");

export const getChallenges = () => authGet<ChallengeListItem[]>("/challenges");

export const getChallenge = (id: string) => authGet<ChallengeDetail>(`/challenges/${id}`);

export const createChallenge = (data: NewChallenge) => authSend<ChallengeDetail>("/challenges", "POST", data);

export const startChallenge = (id: string) => authSend<ChallengeDetail>(`/challenges/${id}/start`, "POST");

export const completeChallenge = (id: string) => authSend<ChallengeDetail>(`/challenges/${id}/complete`, "POST")



export const joinChallenge = (challengeId: string) => authSend<Membership>("/memberships", "POST", {challengeId});

export const leaveChallenge = (membershipId: string) =>  authSend<void>(`/memberships/${membershipId}`, "DELETE");

export const approveMembership = (membershipId: string) => authSend<Membership>(`/memberships/${membershipId}/approve`, "POST");


export const logProgress = (challengeId: string, amount: number, note?: string) => authSend<ProgressEntry>("/progress-entries", "POST", {challengeId, amount, note});

export const deleteProgress = (entryId: string) => authSend<void>(`/progress-entries/${entryId}`, "DELETE");