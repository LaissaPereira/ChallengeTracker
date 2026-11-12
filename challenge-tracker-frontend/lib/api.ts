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
        return(await response.json()) as T;
    }
    if(response.status === 401){
        throw new ApiError("Invalid email or password", 401);

    }
    if(response.status == 429){
        throw new ApiError("Too many requests. Please try again later", 429);
    }
    if(response.status === 400) {
        const body = await response.json().catch(() => null);
        const messages = body?.errors ? (Object.values(body.errors as string[][]).flat()) : [];
        throw new ApiError("Bad Request", 400, messages);
    }
    throw new ApiError(`Unexpected error: ${response.statusText}`, response.status);
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

async function authGet<T>(path: string): Promise<T> {
    const token = await getToken();
    if(!token) throw new ApiError("Not authenticated", 401);
    return request<T>(path, {headers: {Authorization: `Bearer ${token}`},});
}

export const getMe = () => authGet<User>("/auth/me");

export const getChallenges = () => authGet<ChallengeListItem[]>("/challenges");