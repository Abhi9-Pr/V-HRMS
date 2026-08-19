# Data Protection — what Vespera collects, why, and for how long

This document is the DPDP (Digital Personal Data Protection Act, 2023) groundwork required by
Phase 3. It describes what personal data the platform holds today, the lawful basis for holding
it, how consent is recorded, and how retention/purge is enforced. It will grow as later phases add
more personal-data-bearing fields (biometric attendance, Aadhaar-based verification, bank payout
details, background-check results, etc.) — every new field that carries personal data must be
added here, described, and given a retention policy before it ships.

## What is collected today, and why

| Category | Fields | Bounded context | Why it's collected |
|---|---|---|---|
| Identity & contact | Name, work email, phone, date of birth | Eis (`Employee`) | Employment record, statutory filings, communication |
| Statutory identifiers | PAN, bank account number | Eis (`Employee.Pan`, `Employee.BankAccount`) | Payroll disbursal and Indian tax/statutory compliance (TDS, PF challans) |
| Account identity | Email, role assignment | IdentityAccess (`User`) | Authentication and authorization |
| Employment history | Department/designation/location changes | Eis (`EmploymentHistory`) | Org-chart accuracy, audit trail for HR decisions |
| Documents | Uploaded ID/contract/certificate files | Eis (`EmployeeDocument`) | Onboarding compliance, verification |

**Not yet collected, deliberately deferred:** Aadhaar numbers and biometric attendance templates.
Neither is modeled in the domain yet — no field exists to collect them into. The PII-protection
mechanism built in this phase (see below) attaches to those fields automatically, with zero
interceptor changes, the moment a later phase (biometric attendance / Aadhaar-based KYC) adds
them as `IPersonalData` value objects. Scaffolding those fields ahead of that phase would violate
AGENTS.md's "do not scaffold future phases" rule, so this document records the intent instead.

## Lawful basis

Employee personal data is processed under the employment relationship (necessary for performance
of the employment contract and compliance with Indian labour/tax law). Optional processing —
photo usage, background checks — requires explicit consent, tracked per employee via
`ConsentRecord` (owned by `Employee`, one row per `ConsentType`: `DataProcessing`,
`BackgroundCheck`, `PhotoUsage`). An employee (or an HR admin acting for them) can withdraw
consent at any time via `ConsentRecord.Withdraw`; withdrawal is timestamped, not deleted, so there
is always a record of what consent existed when.

## How PII is protected

Two independent mechanisms, at different layers, both described in item 5 of the Phase 3 brief:

1. **Encryption at rest.** `Employee.Pan` and `Employee.BankAccount` are stored encrypted via
   `IPiiProtector` (ASP.NET Core Data Protection in production). The database never holds the
   clear PAN or bank account number — only ciphertext. Reads that legitimately need the plain
   value (e.g. generating a statutory filing) go through the normal read path, which
   transparently decrypts; reads that only need to *display* the value use
   `MaskedProjection`/the value objects' own `Masked()` method instead of decrypting at all.
2. **Redacted audit trail.** Every insert/update/delete is captured as an `AuditLog` row
   (`AuditLogInterceptor`). Any property whose declared type implements `IPersonalData`
   (`PanNumber`, `BankAccountNumber`, and any future PII value object) — or that carries the
   `[Pii]` attribute, for PII not modeled as a value object — is written to the audit row as a
   one-way SHA-256 hash, never in clear. The audit trail can prove *that* a PII field changed and
   correlate identical values, without ever holding the value itself.

## Retention

`RetentionPolicy` (tenant-scoped, `Vespera.Domain.Compliance`) is the source of truth for how long
a category of data is kept and what happens once that window closes (`Purge` or `Anonymize`).
Seeded defaults for the demo tenant:

| Category | Window | Action |
|---|---|---|
| `Employee.Document` | 7 years (2555 days) | Anonymize |
| `Attendance.Punch` | 3 years (1095 days) | Purge |
| `AuditLog` | 6 years (2190 days) | Purge |

`RetentionPurgeHostedService` runs daily and enforces every policy that has a registered
`IRetentionTarget` for its category. Today that's `AuditLog` only (`AuditLogRetentionTarget`) — a
policy for a category with no registered target (`Employee.Document`, `Attendance.Punch`) exists
as configuration, ready for the slice that introduces real enforcement for that category, per the
same "don't scaffold ahead of the phase that owns the feature" principle as the Aadhaar/biometric
note above. Adding enforcement for a new category is a single new `IRetentionTarget`
implementation plus one DI registration — `RetentionPurgeHostedService` itself never changes.

The retention sweep reads across all tenants via `IReadRepositoryAdmin<RetentionPolicy>`
(`IgnoreQueryFilters`) deliberately — it is one of the few legitimate system-wide callers of that
escape hatch (see `docs/CONTRIBUTING-slices.md` and `QueryFilterEscapeHatchTests`, which fails the
build if `IgnoreQueryFilters` is called from anywhere other than `ReadRepositoryAdmin`).
