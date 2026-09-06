using Xunit;

// Every test class here spins up its own VesperaWebApplicationFactory, and host startup runs
// EnsureCreatedAsync (builds the EF Core model + SQLite schema from it) + DevelopmentSeeder before
// TestServer becomes available (see Program.cs: InitializeVesperaDatabaseAsync runs before
// app.Run()). xUnit's default parallelization runs each test class as its own collection
// concurrently.
//
// Two distinct causes were found for the same symptom ("NOT NULL constraint failed:
// SalaryStructureLines.Id"), depending on which database provisioner DatabaseProvisionerSelector
// picks for the environment: on a machine where Docker is reachable, every factory instance was
// sharing one reused Postgres database (fixed separately — see DockerDatabaseOptions.DatabaseName
// and VesperaWebApplicationFactory's per-instance _dockerDatabaseName). On GitHub Actions' hosted
// runners, Docker is not selected and the SQLite fallback is used instead — which already had a
// unique file per factory instance, yet the race still reproduced there (confirmed via CI logs:
// same error, but a SqliteException, not a Postgres one). That points to a genuine EF Core
// thread-safety gap in owned-collection shadow-key generation during concurrent, first-ever
// model/schema builds for the VesperaDbContext type across many separate SQLite-backed hosts.
// Disabling collection parallelization for this assembly removes that concurrency entirely,
// regardless of which provisioner ends up in use — the Docker-side database-naming fix alone is
// not sufficient on its own.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
