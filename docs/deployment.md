# CI/CD pipeline: images, staging, and "production"

`.github/workflows/ci.yml` runs, on every push/PR to `master`: build → test (unit + architecture,
all via `dotnet test Vespera.sln`) → coverage gate (Domain ≥90%, Application ≥80%) → security scan
(NuGet + npm vulnerable-dependency audit, gitleaks secret scan; SAST is CodeQL, `codeql.yml`) →
docker build. On a push to `master` specifically, three more jobs run in sequence: image
publish → staging deploy + smoke/E2E tests → a manually-approved production promotion. This doc
covers those last three, since there's no real cloud host in the picture — read this before
assuming "production deploy" means what it usually means.

## Why there's no real host

Nothing in this repo talks to AWS/Azure/GCP, a Kubernetes cluster, or a VPS — no credentials, no
Terraform, no k8s manifests. Wiring the pipeline to a specific host without one existing would
mean inventing infrastructure decisions (which cloud, which region, what sizing) with no actual
constraints to make them against. So this pipeline stops at the honest boundary: publishing
versioned, tested, immutable container images to GitHub Container Registry (GHCR). Any real host —
a VPS, a k8s cluster, whatever gets stood up later — deploys by pulling those images and running
`docker compose` (or a translation of it) against them. That's also why `docker-compose.yml`
itself remains the actual "one command, seeded working app" answer for the engagement's own
"done when" bar; these CI jobs are the automated proof that the images it *would* pull are good,
not a replacement for it.

## Image tagging

Every push to `master` that passes `build-and-test` produces, for both `vespera-api` and
`vespera-client`:

- `ghcr.io/<owner>/vespera-{api,client}:sha-<7-char-sha>` — immutable, one per commit.
- `ghcr.io/<owner>/vespera-{api,client}:edge` — always the latest `master` build; this is what
  `deploy-staging` pulls.

PRs also build both images (build-only, `docker-build-push` job) to verify the Dockerfiles still
work, but never push — an untrusted fork PR can't publish to the registry. `<owner>` is
`github.repository_owner` lowercased (GHCR image refs must be lowercase; the repo owner isn't).

## Staging: `deploy-staging` job

Pulls the `sha-<sha>` images just pushed (not `edge` — pins to the exact commit this workflow run
is testing, avoiding a race with a second concurrent push) via `docker-compose.staging.yml`, an
overlay on `docker-compose.yml` that swaps the `api`/`client` services' effective image from a
local build to the GHCR pull. See that file's own header comment for why `--no-build` is required
when using it (the base file's `build:` blocks are still present after the merge — compose doesn't
drop a key just because another file sets a different key alongside it).

Once `/health/ready` responds (proves database/outbox/storage wiring, not just process liveness),
two things run against the live stack:

1. **`tools/smoke-test.sh`** (`.ps1` for local Windows use) — fast curl-based checks: readiness,
   tenant lookup, login, one authenticated call, client root. Seconds, not minutes — the fast gate
   that fails fast before spending time on the E2E suite.
2. **The Playwright suite** (`Vespera.Client/e2e/`) — the actual critical-journey specs (punch,
   leave, payroll run, payslip download, departments, dashboard) run for real against this
   deployed stack, not against a locally-run dev server. Their `API_BASE_URL` constants and
   `playwright.config.ts`'s `baseURL` default to the local HTTPS dev-server target
   (`https://localhost:7095` / `http://localhost:4200`) for everyday local runs, but read
   `E2E_API_BASE_URL` / `E2E_BASE_URL` env vars first — this job sets both to the plain-HTTP
   docker-compose ports (`http://localhost:8080` / `http://localhost:4200`).

The stack is always torn down (`docker compose down -v`) afterward, win or lose, so nothing leaks
between runs. Onboarding has no frontend yet (a known, documented gap — see `docs/testing.md`), so
it isn't part of this suite; everything else runs.

## Production: `deploy-production` job

`environment: production` is what makes this a *gated* deploy — GitHub blocks the job until a
required reviewer approves it in the Actions UI. **That protection rule is not in this file**: a
repo admin has to configure it once, in Settings → Environments → `production` → add required
reviewers. Without that one-time setup, `environment: production` alone doesn't block anything.

On approval, the job doesn't rebuild — it runs `docker buildx imagetools create` to copy the exact
image manifest that `deploy-staging` just smoke-tested and E2E-tested onto two new tags:
`production` and `latest`. This guarantees byte-for-byte parity between what staging validated and
what "production" points at; there's no rebuild step in between where something could drift.

A future real deployment target — when one exists — hooks in at exactly this point: add a step
after the promotion that tells that host (via SSH, a webhook, a cloud provider's deploy API,
whatever fits) to pull `ghcr.io/<owner>/vespera-{api,client}:production` and restart. Until then,
that tag is the deliverable: a human-approved, staging-proven, immutable image any future host can
be pointed at with one `docker compose pull && docker compose up -d`.
