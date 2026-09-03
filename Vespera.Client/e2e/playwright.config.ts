import { defineConfig } from '@playwright/test';

// Deliberately no `webServer` block: a real end-to-end run needs both `dotnet run --project
// Vespera.Api` and `npm start` up first (see docs/CONTRIBUTING-frontend.md) — Playwright can't
// own the .NET process, so it isn't asked to own the Angular one either, to keep "how do I run
// this" the same for both halves of the stack.
export default defineConfig({
  testDir: '.',
  timeout: 30_000,
  retries: 0,
  use: {
    // Matches both the local `npm start` dev-server port and the docker-compose (and
    // docker-compose.staging.yml) client port mapping, so no override is needed in CI — see
    // docs/deployment.md.
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'retain-on-failure',
  },
});
