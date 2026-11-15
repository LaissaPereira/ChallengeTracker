# ChallengeTracker

ChallengeTracker lets people create challenges, join them, log daily progress and compete on a leaderboard. The repository holds an ASP.NET Core Web API and a Next.js frontend.

## Features

- Register and log in (JWT authentication, ASP.NET Core Identity)
- Create public or private challenges and move them through `open` → `running` → `completed`
- Join a challenge (private challenges require the owner's approval), leave it, or cancel a request
- Log, update and delete daily progress entries
- Dashboard of challenges and a per-challenge leaderboard
- Rate limiting on login, registration and per-user requests
- Centralised error handling that returns problem details

## Tech stack

| Part | Technology |
| --- | --- |
| API | .NET 10, ASP.NET Core minimal APIs, Entity Framework Core 10, Identity, JWT bearer auth, Scalar / OpenAPI |
| Database | SQLite (local development), SQL Server 2022 (Docker) |
| Frontend | Next.js 16, React 19, Tailwind CSS 4, TypeScript |
| Tests | xUnit unit and integration tests (`ChallengeTrackerApi.Tests`) |

## Repository layout

```text
ChallengeTrackerApi/         Web API (endpoints, services, EF Core models and migrations)
ChallengeTrackerApi.Tests/   Unit and integration tests
challenge-tracker-frontend/  Next.js app
docker-compose.yml           API + SQL Server
.env.example                 Variables used by docker-compose
.github/rulesets/            Branch protection rules
```

## API overview

| Area | Endpoints |
| --- | --- |
| Auth | `POST /auth/register`, `POST /auth/login`, `GET /auth/me` |
| Challenges | `GET /challenges`, `GET /challenges/{id}`, `POST /challenges`, `POST /challenges/{id}/start`, `POST /challenges/{id}/complete` |
| Memberships | `POST /memberships`, `DELETE /memberships/{id}`, `POST /memberships/{id}/approve` |
| Progress entries | `POST /progress-entries`, `PUT /progress-entries/{id}`, `DELETE /progress-entries/{id}` |
| Leaderboards | `GET /leaderboards/challenges/{id}` |

In Development the interactive API reference is at `http://localhost:5040/scalar/v1`.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) and npm (for the frontend)
- Docker (optional, for the containerised setup)

### Run the API locally

The API needs a JWT signing key (at least 32 bytes). In Development it also needs a seed password, because demo data is seeded on startup. Store both as user secrets:

```bash
cd ChallengeTrackerApi
dotnet user-secrets set "Jwt:Key" "<at-least-32-random-bytes>"
dotnet user-secrets set "SeedData:Password" "<demo-user-password>"
dotnet run
```

The API listens on `http://localhost:5040`. In Development it uses SQLite (`challenge.db`), applies migrations automatically and seeds the demo users `owner@challenge.local` and `member@challenge.local` with the seed password.

### Run the frontend

```bash
cd challenge-tracker-frontend
echo "API_URL=http://localhost:5040" > .env.local
npm install
npm run dev
```

The app runs at `http://localhost:3000`. The API allows the origins `http://localhost:3000` and `http://localhost:5173` in Development.

### Run with Docker

```bash
cp .env.example .env   # then replace every placeholder
docker compose up --build
```

This starts the API on `http://localhost:8080` (change with `API_PORT`) and SQL Server, applies migrations and seeds the demo data.

| Variable | Purpose |
| --- | --- |
| `SQLSERVER_SA_PASSWORD` | SQL Server `sa` password |
| `JWT_KEY` | JWT signing key, at least 32 bytes |
| `SEED_PASSWORD` | Password for the seeded demo users |
| `CORS_ORIGIN` | Frontend origin allowed by CORS |
| `API_PORT` | Host port for the API (default `8080`) |

Never commit a real `.env` file.

## Configuration

| Setting | Description |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Database connection string |
| `Database:Provider` | `Sqlite` (default) or `SqlServer` |
| `Database:ApplyMigrations` | Apply EF Core migrations on startup (always on in Development) |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresInMinutes` | JWT settings; the key is required |
| `Cors:AllowedOrigins` | Allowed frontend origins; required |
| `SeedData:Enabled`, `SeedData:Password` | Demo data seeding (always on in Development) |
| `RateLimiting:*` | Permit limits and windows for login, register and per-user requests |

## Tests

```bash
dotnet test
```

## Database migrations

The repository pins `dotnet-ef` as a local tool:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project ChallengeTrackerApi
```

## Branching strategy

- `main` — stable, production-ready code. Protected: PRs required, linear history, no force pushes.
- `dev` — integration branch for ongoing work. Protected: PRs required, linear history, no force pushes.
- `feature/*` — short-lived branches for individual features, branched from `dev` and merged back via PR.

## Repository governance

Branch rulesets are defined under [.github/rulesets](.github/rulesets) and imported into GitHub (Settings > Rulesets).
