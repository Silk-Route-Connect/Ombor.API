# CLAUDE.md — Ombor backend

**.NET / EF Core / SQL Server API for Ombor** — a warehouse and business-management system for small businesses in Uzbekistan. The product's differentiator is a dispute-grade audit trail for debt, payments, and settlement; the backend is where that trail is actually computed and enforced. UZS-only, multi-organization, multi-user.

This file is the operating contract for every backend session. It points to decision docs rather than restating them; when this file and a decision doc conflict, the doc wins — raise it.

---

## Recon before constraint-writing

A data symptom is not a code bug until recon confirms it. Before scoping a fix that adds a constraint, gate, or guard, run a read-only audit of the actual code path. Bad rows in a list often trace to seed/import data that bypassed the service layer, not to a missing rule in the live write path — and "fixing" the already-correct path wastes a session and risks regressions. Recon first; let the audit, not the symptom, define the fix.

## Source-of-truth documents

Shared canon lives in the **sibling checkout `../Ombor.Docs`** (distribution model DR-17) — read-only from here: propose canon edits, never apply them. Access is granted by `.claude/settings.json` → `permissions.additionalDirectories`.

| Document                              | When to read                                                          | What                                                                                                                                                                                      |
| ------------------------------------- | --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `../Ombor.Docs/business-rules.md`     | Any task touching domain behavior                                     | Relevant rules + Domain model + Enum reference. **The spec.**                                                                                                                             |
| `../Ombor.Docs/mvp-plan.md`           | Start of any feature task                                             | The current-task slice — scope and done-ness.                                                                                                                                             |
| `../Ombor.Docs/product-brief.md`      | When a decision needs its reasoning                                   | "Core design decisions" section.                                                                                                                                                          |
| `../Ombor.Docs/decision-log.md`       | Before questioning or reopening any settled choice                    | The row + its revisit trigger.                                                                                                                                                            |
| `../Ombor.Docs/operating-code.md`     | Once per session, before writing any code                             | Cross-repo Code rules: file structure & size, comments, quality bar, reuse, git, session discipline, diagnostics.                                                                         |
| `docs/backend-conventions.md`         | Writing any code                                                      | The craft doc: layers, EF/validation/audit patterns, test philosophy (code-verified 2026-07-14).                                                                                          |
| `docs/backend-gaps.md`                | Any v2/fix planning; checking whether a known gap or open item exists | The verified gap list + the open out-of-band items + incoming contract gaps harvested from the retired delta queue.                                                                       |
| `../Ombor.Web/docs/frontend-gaps.md`  | Start of any contract/endpoint work                                   | The **incoming** view: live FE↔DTO divergences and new FE→backend gaps, recorded as F-items with contract evidence.                                                                     |
| `docs/frontend-fixes.md`              | Historical reference only                                             | The M0–M7-era outgoing queue, consumed by the 2026-07-05 contract alignment; the live divergence list is `frontend-gaps.md`.                                                              |

The M0–M7-era records (redesign plan, both audits, complexity notes, `backend-contract.md`) were retired 2026-07-14 — git history preserves them; tombstones live in `../Ombor.Docs/README.md` → «Retired names». Never treat them as current state.

When a served shape contradicts a business rule, the **rule wins** and the contract is corrected — flag it, don't silently follow either.

---

## Architecture

Clean architecture, five source projects + three test projects:

```
src/
  Ombor.API            Controllers, middleware, composition root, ProblemDetails wiring
  Ombor.Application    Services (heavy business logic lives here), validation, orchestration
  Ombor.Contracts      DTOs only — request/response shapes
  Ombor.Domain         Entities, domain enums, domain rules
  Ombor.Infrastructure EF Core: DbContext, entity configurations, interceptors, persistence
tests/
  Ombor.Tests.Common       Shared test logic
  Ombor.Tests.Integration  Heavy integration coverage (the primary suite)
  Ombor.Tests.Unit         Thin, focused unit tests
src/Ombor.TestDataGenerator  Test/seed data for demo, staging, dev, and integration/CI
```

**Request flow:** Controller → Service → (validation + business logic) → EF Core. Controllers stay thin; the weight lives in Application services, written clean and maintainable. Each entity has its own EF configuration; cross-cutting persistence concerns go through interceptors.

Stack: .NET 8 (net8.0), EF Core 8.0.20, SQL Server, FluentValidation 11 (validators auto-registered, invoked in services via `IRequestValidator`), manual mapping (extension methods in `Application/Mappings` — no AutoMapper/Mapster), JWT bearer auth. `ProblemDetails`/`ValidationProblemDetails` produced by `IExceptionHandler` implementations (not filters); the automatic MVC 400 is suppressed — all validation flows through FluentValidation. Enums serialize as strings; JSON is camelCase. One interceptor: `AuditSaveChangesInterceptor`. Multi-tenancy: global query filter over `IOrganizationScoped` entities + organization stamping on save (organization from JWT claim). Full conventions in `docs/backend-conventions.md`.

## Commands

| Action             | Command                                                                                                                                    |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Build              | `dotnet build Ombor.sln`                                                                                                                   |
| Run locally        | `dotnet run --project src/Ombor.API` (http → :5062, https → :7015; auto-migrates + seeds on startup)                                       |
| Test (all)         | `dotnet test Ombor.sln`                                                                                                                    |
| Test (unit)        | `dotnet test tests/Ombor.Tests.Unit` (no external deps)                                                                                    |
| Test (integration) | `dotnet test tests/Ombor.Tests.Integration` (**requires Docker** — Testcontainers spins up SQL Server 2022)                                |
| Add migration      | `dotnet ef migrations add <Name> --project src/Ombor.Infrastructure --startup-project src/Ombor.API`                                       |
| Apply migration    | Startup auto-runs `MigrateAsync()`; manual: `dotnet ef database update --project src/Ombor.Infrastructure --startup-project src/Ombor.API` |

Run-on-demand, not a long-running daemon. Dev DB persists across restarts (seeded via `Ombor.TestDataGenerator`). **Migrations:** Code may draft and apply to local/dev; production apply is human-gated; never run destructive ops against the production connection string.

## Hard rules

1. **Immutable events get no mutation paths.** No PUT/DELETE on Transaction, Payment, PaymentComponent, PaymentAllocation, Payroll, StockAdjustment, Transfer — at the service and endpoint level. Corrections are counter-events (business-rules §A). The backend rejects such mutations even if a request is crafted.
2. **Everything money/stock is server-computed and served** (rules 8, 12): balances, totals, ages, counts, running balances, WAC. Never a stored balance column that can drift; never expect the client to derive these. A disputed figure must be fully explainable from the event ledger.
3. **Negative stock is impossible** (rule 20): sale lines, decrease adjustments, transfer lines, and order delivery are hard-blocked below zero, returning 400 ValidationProblemDetails with available-vs-requested detail.
4. **Atomicity:** stock + money + ledgers move in one transaction; a transfer updates both warehouses atomically. Partial application must be impossible.
5. **Organization scoping is enforced by the shared mechanism, not per-endpoint filters** (rule 34) — a new endpoint cannot silently skip it. The entity is `Organization`/`OrganizationId`.
6. **Archive is the default; the only hard-delete is reference-gated** (rules 29–32, DR-20). Product/Partner/Wallet/Warehouse (and Category) each serve `IsDeletable` and expose a DELETE that returns **409 when referenced** — archive instead — deleting only when unreferenced. Archived rows with residual money/stock still count in totals.
7. **UZS-only** (rule 33): no currency/exchange-rate fields on new contracts. The legacy currency machinery is frozen — not used, not extended.
8. **No scope additions** beyond `mvp-plan.md` without an explicit decision (rule 36). Surface; don't build.

## Definition of done (every change)

- **Build passes; full test suite passes.** Both are required — a change isn't done until `dotnet test` is green.
- New behavior is covered per the test philosophy: **heavy integration tests** on the money/stock invariants (WAC, source=allocation, atomicity, negative-stock blocks, organization isolation), **thin focused unit tests** elsewhere. Use `Ombor.TestDataGenerator` for setup.
- XML docs on DTOs and public classes. Comments only where they explain a **decision, reason, or tricky part** — never narrate what the code does.
- **Run the full suite as one batch, not isolation-only, and assert behavior.** Shared-context bleed between tests can hide failures that surface only in a full-batch run (the EndpointTestsBase shared-DbContext lesson) — a green isolated test is not proof. Verify the new behavior actually changed, not just that it compiles and green-passes.

## Git, session discipline, diagnostics

The cross-repo rules live in **`../Ombor.Docs/operating-code.md`** — git rules, stop-and-ask session discipline, no unilateral deviations, blocker surfacing, and Sentry/PostHog/SQL diagnostics routing. Read once per session. Backend-specific: data questions run SQL per the migration policy above (never against production destructively); recon-before-constraint-writing (top of this file) extends to production symptoms — the Sentry issue, not the report, defines the fix.

---

## Repo state *(maintained — update when it changes)*

The **M0–M7 redesign is complete** (as of 2026-06-22): `Tenant`→`Organization` rename + multi-tenancy (global filter + stamping), audit interceptor, schema corrections (M0); Wallets + org-setup seeding (M1); the source/allocation payment model (M2); partner balance + ledger (M3); WAC consolidation + warehouses + stock adjustments + transfers + movement read models (M4); orders with delivery→Sale promotion (M5); the Debts + Dashboard read models (M6); Settings — org profile + user management + per-user language (M7). Per-milestone detail lived in the redesign plan (retired 2026-07-14; git history). Build + unit + full integration suites green via Podman.

**Persisted document numbering shipped 2026-07-15 (DR-21):** Transaction/Payment/Order carry a per-organization sequential `Number` (one transaction series across sub-types), allocated atomically via `INumberSequenceAllocator` and served bare (the FE prepends «№»). This resolved the former provisional type+id numbering.

**Documented, deliberately-deferred follow-ups** (not bugs): the order `Returned` transition is a bare status flip pending the refund-drives-return model (M5 open note).

**Live trackers:** `docs/backend-gaps.md` is the v2/fix tracker — the verified gap list plus the open out-of-band items (🔴 committed-secrets rotation, error-key casing) and the incoming contract gaps harvested 2026-07-14 from the retired delta queue. New FE→backend gaps arrive as F-items in `../Ombor.Web/docs/frontend-gaps.md`. Docs migrated 2026-07: shared canon lives in `../Ombor.Docs`; the M0–M7-era records (incl. `backend-contract.md`) were retired 2026-07-14 — git history preserves them.
