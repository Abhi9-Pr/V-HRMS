# 0005. Deployment target: GHCR + docker-compose, no cloud host

**Status:** Accepted

## Context

The CI pipeline needed a staging deploy and a gated production deploy (`.github/workflows/ci.yml`
`deploy-staging`/`deploy-production` jobs). No cloud account, Kubernetes cluster, or Terraform
config exists anywhere in this repo — inventing one (which provider, which region, what sizing)
would mean making infrastructure decisions with no real constraint to make them against, and no
way to verify they're right.

## Decision

Stop at the honest boundary: publish versioned, immutable images to GitHub Container Registry
(needs only the workflow's own `GITHUB_TOKEN`, no new secret), stand up an ephemeral staging stack
on the CI runner itself via `docker-compose.staging.yml` (an overlay that swaps `docker-
compose.yml`'s `api`/`client` `build:` for the just-pushed `image:`), and run the smoke test +
full Playwright suite against it. Production is a manual-approval GitHub Environment gate that
promotes the exact staging-tested image digest (via `docker buildx imagetools create` — a manifest
copy, not a rebuild) to `production`/`latest` tags. There is no step that deploys those tags to a
running host, because no host exists — see `docs/deployment.md`.

## Alternatives considered

- **Wire up a specific cloud target now** (e.g. an Azure App Service or a small VPS), inventing the
  concrete shape. Rejected: this is guessing at infrastructure requirements (cost tolerance,
  region, scaling needs) nobody has stated, for a target that might not match what actually gets
  used later — better to build the pipeline up to the real boundary of what's known and stop
  cleanly there.
- **Skip staging/production jobs entirely, stop at "images build."** Rejected: this throws away
  real, checkable value — an ephemeral staging deploy that actually runs the Playwright suite against
  a freshly-pulled image is a meaningfully stronger CI guarantee than "the Dockerfile parses,"
  and the production gate's image-promotion step is itself a real, usable deliverable (an
  immutable, approved artifact any future host can point at).
- **Self-hosted runner as the "staging" host** (long-lived, not ephemeral). Rejected: adds an
  operational dependency (a runner someone has to keep alive and patched) for a target this
  engagement has no standing infrastructure to host.

## Consequences

- "Production" in this repo currently means an immutable, human-approved GHCR image family, not a
  live, reachable deployment. A real host, when one exists, hooks in by adding one step after the
  `deploy-production` promotion that tells that host to pull `:production` and restart — the
  pipeline is built to that seam deliberately.
- Every staging run is fully ephemeral (torn down with `docker compose down -v` regardless of
  outcome) — there's no persistent staging environment to inspect after the fact beyond the
  workflow's own logs and the uploaded Playwright report artifact.
- `docker compose up -d --build` (documented as the "done when" bar: a new machine with only Docker
  can clone and reach a seeded app) remains the actual runnable target for anyone without access to
  the GHCR images — the CI pipeline validates the same Dockerfiles that path uses, it doesn't
  replace it.
