import {NextResponse, type NextRequest} from "next/server";

export function proxy(req: NextRequest) {
    const hasToken = req.cookies.has("auth_token");
    const { pathname } = req.nextUrl;
    const isAuthPage = pathname.startsWith("/login") || pathname.startsWith("/register");

    if(!hasToken && !isAuthPage) return NextResponse.redirect(new URL("/login", req.url));
    if(hasToken && isAuthPage) return NextResponse.redirect(new URL("/", req.url));

}

export const config = { matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"] };