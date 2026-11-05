# ChallengeTracker

ChallengeTracker API and related tooling.

## Branching strategy

- `main` — stable, production-ready code. Protected: PRs required, linear history, no force pushes.
- `dev` — integration branch for ongoing work. Protected: PRs required, linear history, no force pushes.
- `feature/*` — short-lived branches for individual features, branched from `dev` and merged back via PR.

## Repository governance

Branch rulesets are defined under [.github/rulesets](.github/rulesets) and imported into GitHub (Settings > Rulesets).
