# Backend conventions — Ombor

**Status:** verified 2026-07-14 against `src/` — a 12-claim spot-check (2026-07-13) of layering, DI, validation, mapping, interceptor, tenancy, controllers, serialization, and tests: 10 claims matched the code, 2 corrected below (tenancy interface name; exception-handler chain). Originally harvested 2026-07-12 from `audit-findings.md` Part 2 (code-traced 2026-06-18), the redesign plan's settled decisions, and `backend-complexity-notes.md`. Items marked ⚠ are known-uncertain or open.
**Last updated:** 2026-07-14

Read once per session before writing code. Codifies the patterns the codebase follows; new code must follow them. Where existing legacy code and this doc disagree, follow this doc for new code and don't refactor legacy outside the task scope. When changing a pattern here seems justified, propose it — don't fork silently. Pair with `../Ombor.Docs/business-rules.md` (the spec), `CLAUDE.md` (hard rules), and `backend-gaps.md` (known gaps).

---

## Layering & flow

```
Controller (Ombor.API) → Service (Ombor.Application) → IApplicationDbContext (EF) → manual Mapping ext. → DTO (Ombor.Contracts)
        ▲ FluentValidation via IRequestValidator ▲         ▲ global query filter + audit interceptor ▲
```

- **Domain** — entities, enums, domain exceptions; no dependencies.
- **Application** — services (the business-logic weight), validators, mappings, interfaces (including `IApplicationDbContext`, so services never reference the concrete context); depends on Contracts + Domain.
- **Contracts** — DTOs only, split `Requests/<Area>` and `Responses/<Area>`.
- **Infrastructure** — EF Core: `ApplicationDbContext`, entity configurations, interceptors.
- **API** — controllers, exception handlers, extensions, composition root. DI composed via per-layer `AddApi` / `AddApplication` / `AddInfrastructure` / `AddTestDataGenerator` extensions.
- `internal sealed` is the default for services, configs, and handlers; `InternalsVisibleTo` exposes internals to the test projects.

## Feature anatomy — where a new endpoint's pieces go

Controller action (API) · service interface + implementation (Application, registered in `AddApplication`) · request/response records (Contracts, per-area folders) · validator under `Application/Validators/<Area>`, named `<Request>Validator` (auto-registered) · mapping extensions in `Application/Mappings` · entity + `IEntityTypeConfiguration` + migration (Infrastructure) · integration/unit tests per the philosophy below. Create all of it; put code where this anatomy says.

## Controllers

- `[ApiController]`, `[Authorize]`, `[Route("api/<plural>")]` — **lowercase, plural** resource routes; no versioning segment. Command actions are POST sub-routes (`archive`, `restore`, order transitions).
- `sealed`, primary-constructor DI. Action methods `async` with the `Async` suffix retained (`SuppressAsyncSuffixInActionNames = false`).
- Inline route constraints (`{id:int:min(1)}`); all binding via **request records** (`[FromQuery]` / `[FromRoute]` / `[FromBody]`), even single-id reads.
- `[ProducesResponseType(...)]` declared per status; XML `<summary>/<param>/<returns>` on every public action (fed to Swagger).
- Controllers stay **thin**: delegate to the service, wrap in `Ok` / `CreatedAtAction` / `NoContent`. The one sanctioned inline guard is the PUT route-vs-body id-mismatch check returning `ProblemDetails`.

## Services

- `internal sealed`, primary-constructor DI of `IApplicationDbContext` + `IRequestValidator`.
- **Validation is the first line of every write:** `await validator.ValidateAndThrowAsync(request)`. ⚠ Historically list `GetAsync` requests were unvalidated — validate any request that carries constraints.
- Not-found via a private `GetOrThrowAsync` → `throw new EntityNotFoundException<TEntity>(id)`.
- Projection: **list queries project inline** in the LINQ `Select` with `AsNoTracking()`; **single-entity reads and writes** use the mapping extensions (`ToDto`, `ToEntity`, `ApplyUpdate`).
- Business logic is procedural in services; entities are mostly anemic (domain methods only where they already exist, e.g. `TransactionRecord.AddPayment`).
- Money/stock orchestration moves **stock + money + ledgers in one transaction** — partial application must be impossible (CLAUDE.md hard rule 4).
- Services follow the shared file-structure rule (`../../Ombor.Docs/operating-code.md`): a service crossing the ~300-line tripwire is split by sub-concern (orchestration vs calculation vs mapping), keeping one public service surface.

## Validation & error shape

- FluentValidation auto-registered (`AddValidatorsFromAssembly`), invoked in services through `IRequestValidator`. The automatic MVC 400 is **suppressed** — all validation flows through FluentValidation.
- Errors are produced by **`IExceptionHandler` implementations** (never filters or per-action code), registered in order: `ValidationExceptionHandler` → `InvalidEnumExceptionHandler` → `EntityNotFoundExceptionHandler` → `ConflictExceptionHandler` → `InvalidOrderStateTransitionExceptionHandler` → `InvalidFileExceptionHandler` → `GlobalExceptionHandler` (`API/Extensions/DependencyInjection.cs` → `AddErrorHandlers`).
- Shapes the frontend depends on: **400** `ValidationProblemDetails` with `Errors = Dictionary<string,string[]>` keyed by property; **404** / **500** `ProblemDetails` (500 message hidden in prod); **409** `ProblemDetails` for reference-gated deletes (Partner, Category).
- JSON conventions: camelCase properties, nulls ignored on write, **enums as strings** via the custom `ValidatingStringEnumConverter` (`Ombor.Contracts/Serialization/`) — it wraps `JsonStringEnumConverter` but throws `InvalidEnumValueException` on an unparseable value, so an invalid enum surfaces as a 400 instead of the built-in converter's null-argument 500.
- ⚠ **Error-key casing drift (open — `backend-gaps.md` → Out-of-band open items):** `Errors` keys come from FluentValidation `PropertyName`s (PascalCase, `Lines[0].Quantity`), not camelCase — don't build anything that depends on key casing until resolved.

## Mapping

Manual, `internal static` extension classes per entity in `Application/Mappings` — `ToDto`, `ToEntity`, `ToCreateResponse`, `ToUpdateResponse`, `ApplyUpdate`. No AutoMapper/Mapster. A new field updates the mapping and the DTO XML docs in the same change.

## Persistence

- One `internal sealed XxxConfiguration : IEntityTypeConfiguration<Xxx>` per entity in `Persistence/Configurations`, auto-applied via `ApplyConfigurationsFromAssembly`; `builder.ToTable(nameof(Xxx))`; shared lengths from `ConfigurationConstants`.
- Cross-cutting persistence concerns go through **interceptors**. The one in place: `AuditSaveChangesInterceptor` — every `IAuditable` change writes an `AuditEntry` (actor, timestamp, entity type + id, action, before/after JSON, organization), with insert ids backfilled in `SavedChanges`. **A new auditable entity implements `IAuditable`;** never hand-roll audit writes.
- Migrations per CLAUDE.md: draft and apply to local/dev freely; production apply is human-gated; never destructive ops against the production connection string.

## Multi-tenancy

Global query filter applied **by reflection over every `IOrganizationScoped` entity**, plus organization stamping on save (organization from the JWT claim). A new tenant-scoped entity implements `IOrganizationScoped` and gets both for free — **never a hand-written per-endpoint filter** (hard rule 5 / rule 34). Cross-organization isolation is covered by integration tests as a first-class invariant.

## Money & stock patterns

- **Everything money/stock is server-computed and served** (rules 8, 12): balances, totals, ages, counts, running balances, WAC. The client never derives them.
- **Balances are derived at read time — nothing is persisted.** `PartnerBalance` is a **SQL view** over the event tables; any query needing a balance joins and aggregates the view (`Partner.Balance` is removed). No stored balance columns anywhere — canon's "computed per read, never stored" (rules 12, 15) holds literally. ⚠ Mechanism corrected from owner input 2026-07-13 — the redesign plan's "persisted projection recomputed in-transaction" wording did not match what shipped; the verification pass should confirm the view mapping and how wallet balances derive.
- **Ledger is derive-on-read** — no `PartnerLedgerEntry` table. The ledger's newest-first running balance must terminate **exactly** at the served partner balance; that reconciliation is what makes a disputed figure defensible.
- **Sign convention:** `balance > 0` = the partner owes us (receivable); `< 0` = we owe them. Consistent everywhere — Partner, Order `customerBalance`, Debt direction, Dashboard.
- **WAC engine:** stock-in events recompute the average (`(oldQty·oldWAC + inQty·inCost)/(oldQty+inQty)`; transfer-in carries the source WAC; Increase adjustments restore at current WAC without changing it); stock-out events leave at current WAC (Sale → COGS; Decrease adjustment → a loss line distinct from COGS). Base-unit movement with package count retained on lines.
- **Negative stock is impossible** (rule 20): sale lines, decrease adjustments, transfer lines, order delivery — hard-blocked with **400 `ValidationProblemDetails`** carrying available-vs-requested detail.

## Read models

- **Debts** is a projection over outstanding transactions — no stored entity. `remaining = total − paid`; direction from transaction type; ages server-computed.
- **Dashboard** is a snapshot computed **from the same source as `/api/debts`** so the two screens reconcile (mvp-plan §2 done-criterion). `period` drives only the revenue KPI and the time series; debt figures are period-independent.
- **Preserve the deliberate definition divergence:** the dashboard's «Просрочено» = receivables aged 31+ days (the aging-bucket definition); `/debts` overdue = past-due-date. They can legitimately disagree — do **not** unify them.

## Immutability enforcement

No PUT/DELETE surface — at endpoint **and** service level — for TransactionRecord, Payment, PaymentComponent, PaymentAllocation, Payroll, StockAdjustment, Transfer (hard rule 1). Corrections are counter-events. Opening balances and opening stock are auditable events, never raw field assignments (rule 16, 22).

## Contract & endpoint conventions

- Transaction creation is a **single `POST /api/transactions`** with the `type` discriminator (Sale / Supply / SaleRefund / SupplyRefund) — **no separate `/refund` endpoint** (settled). The request is **`multipart/form-data`** (a `payload` JSON part + attachment file parts), since transactions carry attachments. ⚠ The frontend mock still exposes `POST /{id}/refund` — reconciliation tracked in the delta queues.
- PUT semantics for nullable references follow the Order-warehouse precedent: **`undefined` = keep existing, `null` = explicit clear, value = set.**
- Reference-gated DELETE (Partner, Category) returns 409 while referenced; `isDeletable` is served and must agree with the guard.

## Testing

- **Heavy integration tests on the money/stock invariants** — WAC, source = settling allocations, atomicity, negative-stock blocks, organization isolation — via Testcontainers SQL Server 2022 (Docker/Podman required). **Thin, focused unit tests** elsewhere.
- Setup through `Ombor.TestDataGenerator`; never bespoke seed paths.
- **Run the full suite as one batch** — shared-context bleed can hide failures an isolated run never sees (the `EndpointTestsBase` shared-DbContext lesson). Assert the behavior actually changed, not merely that the build is green.

## Docs & comments

XML docs on DTOs and public classes. Comments only where they explain a **decision, reason, or tricky part** — never narrating what the code does. All code, comments, and commits in English.
