"use server";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, approveMembership, completeChallenge, createChallenge, deleteProgress, joinChallenge, leaveChallenge, logProgress, startChallenge, type NewChallenge } from "../../lib/api";


export type ActionState = { error?: string; fieldErrors?: string[]; values?: Record<string, string> } | undefined;

async function run(work : () => Promise<unknown>, ...paths: string[]): Promise<ActionState> {
    try {
        await work();
    } catch (error) {
        if (error instanceof ApiError) {
            if (error.status === 401) redirect("/logout");
            return { error: error.message};
        }
        throw error;
    }
    paths.forEach(path => revalidatePath(path));
    return {};
}



export const joinChallengeAction = async (challengeId: string) =>
    run(() => joinChallenge(challengeId), "/", `/challenges/${challengeId}`);

export const approveMembershipAction = async (membershipId: string, challengeId: string) =>
    run(() => approveMembership(membershipId), "/", `/challenges/${challengeId}`);

export const startChallengeAction = async (id: string) =>
    run(() => startChallenge(id), `/challenges/${id}`, "/");

export const completeChallengeAction = async (id: string) =>
    run(() => completeChallenge(id), `/challenges/${id}`, "/");

export const deleteProgressAction = async (entryId: string, challengeId: string) =>
    run(() => deleteProgress(entryId), `/challenges/${challengeId}`);

export async function leaveChallengeAction(membershipId: string) {
    const result = await run(() => leaveChallenge(membershipId), "/");
    if (result?.error) return result;
    redirect("/");
}

export async function logProgressAction(challengeId: string, _prev: ActionState, formData: FormData): Promise<ActionState> {
    const values = { amount: String(formData.get("amount") ?? ""), note: String(formData.get("note") ?? "") };
    const amount = Number(values.amount);
    if (!Number.isFinite(amount) || amount <= 0) return { error: "Amount must be greater than 0", values };
    const note = values.note.trim();
    const result = await run(() => logProgress(challengeId, amount, note || undefined), `/challenges/${challengeId}`);
    return result?.error ? { ...result, values } : result;
}

export async function createChallengeAction(_prev: ActionState, formData: FormData): Promise<ActionState> {
    const values = Object.fromEntries(
        ["title", "description", "visibility", "startDate", "endDate"].map((key) => [key, String(formData.get(key) ?? "")]),
    );
    const data: NewChallenge = {
        title: values.title.trim(),
        description: values.description.trim() || undefined,
        visibility: values.visibility === "private" ? "private" : "public",
        startDate: values.startDate,
        endDate: values.endDate,
    }
    let id : string;
    try {
        id = (await createChallenge(data)).id;
    } catch (error) {
        if (error instanceof ApiError) {
            if (error.status === 401) redirect("/logout");
            return { error: error.message, fieldErrors: error.fieldErrors, values };
        }
        throw error;
    }
    revalidatePath("/");
    redirect(`/challenges/${id}`);
}