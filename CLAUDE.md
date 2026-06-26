# CLAUDE.md — Ombor backend

**.NET / EF Core / SQL Server API for Ombor** — a warehouse and business-management system for small businesses in Uzbekistan. The product's differentiator is a dispute-grade audit trail for debt, payments, and settlement; the backend is where that trail is actually computed and enforced. UZS-only, multi-organization, multi-user.

This file is the operating contract for every backend session. It is **thin and provisional** — sections marked _(confirm in audit)_ are filled by the first audit session, not assumed. It points to decision docs rather than restating them; when this file and a decision doc conflict, the doc wins — raise it.

---

## Recon before constraint-writing

A data symptom is not a code bug until recon confirms it. Before scoping a fix that adds a constraint, gate, or guard, run a read-only audit of the actual code path. Bad rows in a list often trace to seed/import data that bypassed the service layer, not to a missing rule in the live write path — and "fixing" the already-correct path wastes a session and risks regressions. Recon first; let the audit, not the symptom, define the fix.

## Source-of-truth documents

| Document                           | When to read                                                            | What                                                                                                                                                              |
| ---------------------------------- | ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docs/business-rules.md`           | Any task touching domain behavior                                       | Relevant rules + Domain model + Enum reference. **The spec.**                                                                                                     |
| `docs/backend-contract.md`         | Any endpoint work                                                       | The target API contract the frontend already calls. **Target, not current** — its REAL/STALE/MOCKED labels are inferences to verify against real code, not facts. |
| `docs/backend-complexity-notes.md` | Any non-trivial endpoint (WAC, payments, balances, orders, read models) | The server-side logic the contract doc's shapes hide. Read alongside the contract.                                                                                |
| `docs/mvp-plan.md`                 | Start of any feature task                                               | The current-task slice — scope and done-ness.                                                                                                                     |
| `docs/product-brief.md`            | When a decision needs its reasoning                                     | "Core design decisions" section.                                                                                                                                  |
| `docs/backend-conventions.md` | Writing any code | The craft doc. Not yet written — until it exists, follow the patterns in docs/audit-findings.md Part 2 (architecture trace) and match the conventions of the file you're editing. |
| `docs/backend-build-plan.md` | Every session | The milestone tracker — current milestone, settled decisions, what's next. Update it at session end. |
| `docs/audit-findings.md` | When a milestone touches an area the audit examined | Ground-truth current state and risks. |

When a contract shape contradicts a business rule, the **rule wins** and the contract is corrected — flag it, don't silently follow either.

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

Stack: .NET 8 (net8.0), EF Core 8.0.20, SQL Server, FluentValidation 11 (validators auto-registered, invoked in services via `IRequestValidator`), manual mapping (extension methods in `Application/Mappings` — no AutoMapper/Mapster), JWT bearer auth. `ProblemDetails`/`ValidationProblemDetails` produced by `IExceptionHandler` implementations (not filters); the automatic MVC 400 is suppressed — all validation flows through FluentValidation. Enums serialize as strings; JSON is camelCase. One interceptor: `AuditSaveChangesInterceptor`. Multi-tenancy: global query filter over `ITenantScoped` entities + tenant stamping on save (tenant from JWT claim). Full conventions in `docs/backend-conventions.md`.

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
6. **Soft-archive, never hard-delete** for Product/Partner/Wallet/Warehouse (rules 29–32); archived rows with residual money/stock still count in totals. The only true DELETE is reference-gated (Partner, Category) → 409 when referenced.
7. **UZS-only** (rule 33): no currency/exchange-rate fields on new contracts. The legacy currency machinery is frozen — not used, not extended.
8. **No scope additions** beyond `mvp-plan.md` without an explicit decision (rule 36). Surface; don't build.

## Definition of done (every change)

- **Build passes; full test suite passes.** Both are required — a change isn't done until `dotnet test` is green.
- New behavior is covered per the test philosophy: **heavy integration tests** on the money/stock invariants (WAC, source=allocation, atomicity, negative-stock blocks, organization isolation), **thin focused unit tests** elsewhere. Use `Ombor.TestDataGenerator` for setup.
- XML docs on DTOs and public classes. Comments only where they explain a **decision, reason, or tricky part** — never narrate what the code does.

## Git rules

- Work on the branch the user currently has checked out. Do not create repository branches; local scratch branches only, never pushed.
- Never switch/rename/rebase/reset the user's branch without explicit instruction.
- Commit under the existing git identity; never modify git config. **No attribution trailers** (no `Co-Authored-By`, no "Generated with").
- Commit format: `(<branch-name>) - <clear imperative summary>`. Small, scoped commits.
- No push/force/history-rewrite unless explicitly asked.

## Session discipline

- Orient before coding: read the docs the task prescribes, state the plan, then implement. One topic per session.
- **Stop and ask** when a fact is missing or a doc contradicts the task — never proceed on an assumption. **Omitting or altering a specified behavior is never a unilateral call** — pause mid-session and ask; end-of-session gap lists are for discoveries that didn't change what you built, not for justifying solo decisions.
- A gap between code and a doc that's outside the current task → report at session end; don't fix silently.

---

## Repo state _(maintained — update when it changes)_

The **M0–M7 redesign is complete** (as of 2026-06-22). Delivered across the milestones: `Tenant`→`Organization` rename + multi-tenancy (global filter + stamping), audit interceptor, schema corrections (M0); Wallets + org-setup seeding (M1); the source/allocation payment model (M2); partner balance + ledger (M3); WAC consolidation + warehouses + stock adjustments + transfers + movement read models (M4); orders with delivery→Sale promotion (M5); the Debts + Dashboard read models (M6); and Settings — org profile + user management + per-user language (M7). Per-milestone detail (decisions, what each slice changed) lives in `docs/backend-redesign-plan.md`; frontend contract mismatches the redesign forces are tracked in `docs/frontend-fixes.md`. Build + unit + full integration suites are green via Podman. **Documented, deliberately-deferred follow-ups** (not bugs): provisional transaction numbering (real per-type sequences are future work — debt/dashboard numbers are derived from type+id); the order `Returned` transition is a bare status flip pending the refund-drives-return model (M5 open note).
