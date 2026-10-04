# Backend conventions — Ombor

**Status:** verified 2026-07-14 against `src/` — a 12-claim spot-check (2026-07-13) of layering, DI, validation, mapping, interceptor, tenancy, controllers, serialization, and tests: 10 claims matched the code, 2 corrected below (tenancy interface name; exception-handler chain). Originally harvested 2026-07-12 from `audit-findings.md` Part 2 (code-traced 2026-06-18), the redesign plan's settled decisions, and `backend-complexity-notes.md`. Items marked ⚠ are known-uncertain or open.
**Last updated:** 2026-10-04

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
- Controllers stay **thin**: delegate to the service, wrap in `Ok` / `CreatedAtAction` / `NoContent`. The one sanctioned inline guard is the PUT route-vs-body id-mismatch check returning `ProblemDetails`, written `if (request is not null && id != request.Id)` so a missing body skips it and reaches the service validator (`IRequestValidator` answers a null request with 400) instead of a null-dereference 500.

## Services

- `internal sealed`, primary-constructor DI of `IApplicationDbContext` + `IRequestValidator`.
- **Validation is the first line of every write:** `await validator.ValidateAndThrowAsync(request)`. ⚠ Historically list `GetAsync` requests were unvalidated — validate any request that carries constraints.
- Not-found via a private `GetOrThrowAsync` → `throw new EntityNotFoundException<TEntity>(id)` — for the resource **in the route** only.
- **Every foreign id in a write body goes through `OwnedReferences`** (`Application/Helpers/OwnedReferences.cs`) right after validation, before mapping: `OwnedReferences.Check().Require(context.Partners, request.PartnerId, nameof(request.PartnerId)).Require(context.Products, request.Lines.Select((l, i) => (l.ProductId, $"Lines[{i}].ProductId"))).ThrowIfMissingAsync()`. It queries the organization-filtered sets and throws one 400 (`validation.failed`) listing every missing/foreign id on its property path. A body reference is never a 404, and never resolved with an unfiltered lookup — the global filter only guards reads; without this check an id from another organization satisfies the FK and corrupts that tenant's ledger (rule 34). A new write endpoint with an id in its body must use it.
- Projection: **list queries project inline** in the LINQ `Select` with `AsNoTracking()`; **single-entity reads and writes** use the mapping extensions (`ToDto`, `ToEntity`, `ApplyUpdate`).
- Business logic is procedural in services; entities are mostly anemic (domain methods only where they already exist, e.g. `TransactionRecord.AddPayment`).
- Money/stock orchestration moves **stock + money + ledgers in one transaction** — partial application must be impossible (CLAUDE.md hard rule 4). That transaction is the one the **organization write lock** opens (see «Concurrency» below), never a bare `BeginTransactionAsync`.
- Services follow the shared file-structure rule (`../../Ombor.Docs/operating-code.md`): a service crossing the ~300-line tripwire is split by sub-concern (orchestration vs calculation vs mapping), keeping one public service surface.

## Validation & error shape

- FluentValidation auto-registered (`AddValidatorsFromAssembly`), invoked in services through `IRequestValidator`. The automatic MVC 400 is **suppressed** — all validation flows through FluentValidation.
- Errors are produced by **`IExceptionHandler` implementations** (never filters or per-action code), registered in order: `ValidationExceptionHandler` → `InvalidEnumExceptionHandler` → `EntityNotFoundExceptionHandler` → `ConflictExceptionHandler` → `InvalidOrderStateTransitionExceptionHandler` → `InvalidFileExceptionHandler` → `SmsDeliveryExceptionHandler` → `UnauthorizedAccessExceptionHandler` → `TooManyRequestsExceptionHandler` → `DbUpdateExceptionHandler` → `GlobalExceptionHandler` (`API/Extensions/DependencyInjection.cs` → `AddErrorHandlers`).
- **No client mistake may reach the client as a 500.** Request paths throw `ValidationException` (field error), `EntityNotFoundException` (route resource), `ConflictException` or an `InvalidFileException` subtype — never `InvalidOperationException` / `ArgumentException` for bad input. `DbUpdateExceptionHandler` is the safety net for a constraint a guard missed: SQL 2601/2627 → 409 `conflict.duplicate`, 547 → 409 `entity.referenced`, 2628/8152 → 400 `validation.failed`; it logs a warning (reaching Sentry) because hitting it means a guard is missing — add the guard.
- Shapes the frontend depends on: **400** `ValidationProblemDetails` with `Errors = Dictionary<string,string[]>` keyed by property; **404** / **500** `ProblemDetails` (500 detail is generic outside Development, with a `traceId`); **409** `ProblemDetails` for reference-gated deletes (Partner, Category); **429** `ProblemDetails` + `Retry-After` for throttling.
- **Error codes (added 2026-10-03).** Every domain error body carries the extension members **`code`** (one of `Domain/Exceptions/ErrorCodes`) and optional **`params`** (camelCase keys the client interpolates), so the UI localizes by code and never shows raw server text. The mechanism is small — reuse it, don't add a parallel one:
  - Throw an exception that implements `ICodedError` (`Code` + `Params`): `AuthenticationFailedException` (401), `TooManyRequestsException` (429, `retryAfterSeconds`), or your own domain exception mapped by a handler. Handlers call `problem.WithCodeFrom(exception, fallback)` / `problem.WithCode(code, params)` (`API/ExceptionHandlers/ProblemDetailsCodeExtensions.cs`).
  - For a **400 with a field error** (e.g. `stock.insufficient`, `auth.phone_taken`) throw `CodedValidation.Failure(propertyName, message, code, params)` (`Application/Validators`), or attach `.WithErrorCode(ErrorCodes.X)` to a validator rule; the first failure with a domain code (it contains a dot) becomes the top-level `code`, its `CustomState` dictionary becomes `params`.
  - Defaults when nothing more specific is thrown: 400 → `validation.failed`, 404 → `entity.not_found`, 409 (`ConflictException`) → `entity.referenced`. A `ConflictException` built with a code carries it instead (`conflict.busy` from the write lock).
  - A uniqueness rule on a form field carries its code on the validator rule (`product.sku_taken` on `SKU`); if the field also has a unique index, register the index in `DbUpdateExceptionHandler.FieldUniqueIndexes` so a race past the check gets the same 400 field error, not a generic 409.
  - Shared coded errors — reuse, don't rebuild: the negative-stock block is thrown by `MoveStockAsync` (`stock.insufficient` with `productName`/`available`/`requested`; pass `propertyFor` when the line path is not `Lines[i].Quantity`); every wallet overdraft goes through `EnsureWalletCanCoverAsync(walletId, amount, propertyName)` or `WalletCalculationExtensions.InsufficientBalance` (`wallet.insufficient_balance`); a settlement in the wrong direction through `TransactionRecord.EnsureSettlableBy(direction, propertyName)` (`payment.direction_mismatch`); upload rejections are `InvalidFileException` subtypes, which carry `file.invalid` / `file.too_large` themselves.
  - A new code is added to `ErrorCodes` **and** to `Ombor.Docs/backend-contracts/conventions.md` in the same change.
- JSON conventions: camelCase properties, nulls ignored on write, **enums as strings** via the custom `ValidatingStringEnumConverter` (`Ombor.Contracts/Serialization/`) — it wraps `JsonStringEnumConverter` but throws `InvalidEnumValueException` on an unparseable value, so an invalid enum surfaces as a 400 instead of the built-in converter's null-argument 500.
- **Error-key casing — PascalCase (ruled 2026-07-19):** validation-400 `Errors` keys are **PascalCase**, straight from FluentValidation `PropertyName`s (e.g. `Lines[0].Quantity`). This is the intentional, documented contract — the frontend keys its error map by PascalCase property paths. Only the `Errors` dictionary keys are PascalCase; the rest of every JSON body stays camelCase.

## Auth & account security (added 2026-10-03)

- **Phone numbers:** every user phone write and lookup goes through `Application/Helpers/PhoneNumbers` (`TryNormalize` / `Canonical` → `+998XXXXXXXXX`; validators use `PhoneNumbers.IsValid`). Never compare a raw client string against `User.PhoneNumber`. Uniqueness checks on phone/email/Telegram use `IgnoreQueryFilters()` — the indexes are global, not per organization.
- **Throttles** count with the atomic `IRedisService.IncrementAsync` (never get-then-set, which parallel requests race past): OTP send budget and wrong-guess cap in `OtpCodeProvider`, the per-phone login lockout in `ILoginThrottle`, per-IP limits as named `[EnableRateLimiting]` policies (`API/Extensions/AuthSecurityExtensions.cs`). A new anonymous endpoint gets a policy. Limits are configured in `OtpSettings` / `AuthSecuritySettings` (defaults in code; Testing raises the IP limits).
- **Sessions:** issue/revoke/prune refresh tokens only through `IRefreshTokenStore`; anything that ends a user's access (deactivation, password change/reset) revokes there and, for deactivation, invalidates `IActiveUserCache` (the bearer-token rule-41 check).
- **Never let a response tell accounts apart** on login/forgot-password: same status, same body, comparable timing.

## Mapping

Manual, `internal static` extension classes per entity in `Application/Mappings` — `ToDto`, `ToEntity`, `ToCreateResponse`, `ToUpdateResponse`, `ApplyUpdate`. No AutoMapper/Mapster. A new field updates the mapping and the DTO XML docs in the same change.

## File uploads

- Go through `IFileService`: `UploadAsync` for document attachments (images or PDF), `UploadImageAsync` / `UploadImagesAsync` for product images and logos (images only). It checks the **content** (magic bytes, `Helpers/FileSignatures`) against the extension, stores under a random GUID name, and returns the detected `ContentType` — store that, never the client's `IFormFile.ContentType`.
- Every upload section is served statically under `/{PublicUrlPrefix}` from `wwwroot/{BasePath}` (`StartupExtensions.UseStaticFiles`) with `nosniff`; the stored URL is relative to the API base. Static files carry no auth — the GUID name is what keeps a URL unguessable, so never build an upload name from user input or a sequential id.
- Cap the number of files per request in the validator (`ValidationConstants.MaxAttachments`).

## Persistence

- One `internal sealed XxxConfiguration : IEntityTypeConfiguration<Xxx>` per entity in `Persistence/Configurations`, auto-applied via `ApplyConfigurationsFromAssembly`; `builder.ToTable(nameof(Xxx))`; shared lengths from `ConfigurationConstants`.
- Cross-cutting persistence concerns go through **interceptors**. The one in place: `AuditSaveChangesInterceptor` — every `IAuditable` change writes an `AuditEntry` (actor, timestamp, entity type + id, action, before/after JSON, organization), with insert ids backfilled in `SavedChanges`. **A new auditable entity implements `IAuditable`;** never hand-roll audit writes. See «Audit & Activity Log» below.
- Migrations per CLAUDE.md: draft and apply to local/dev freely; production apply is human-gated; never destructive ops against the production connection string.

## Audit & Activity Log (added 2026-10-04, scope-2)

- **What is audited (rules 26–28):** every money/stock event and all mutable master data — Product, Category, Partner, Wallet, Warehouse, Employee, Template, Order, Organization, User — plus the parts of a record: transaction/order/transfer lines, template items, payment components/allocations, order status events, stock rows. A new mutable entity implements `IAuditable`; a part of another record implements **`IAuditableChild`** (`AuditParent => new(typeof(Parent), ParentId)`), so the parent's «История» includes it.
- **The interceptor is generic** (`AuditChangeReader`): it diffs every audited column with EF's own value comparers (so `100.00` from SQL equals `100` from a request), folds complex properties and owned types into the owner as dotted columns (`Packaging.Size`, `ContactInfo.Email`), stores enums by name, skips an update whose only changes are excluded columns, and turns a flip of `IsArchived` into `Archived` / `Restored`.
- **What never reaches the log** is decided in one place, `Domain/Common/AuditFieldPolicy`: ids, `OrganizationId`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `CreatedById` (the actor is on the row), plus any property marked **`[NotAudited]`**. Secrets use `[NotAudited(MaskedAs = "Password")]` — a change is logged by that name with no values. A new secret or token column on an audited entity **must** carry the attribute.
- **Operations:** rows written while serving one HTTP request share an `OperationId` (one per save outside a request). Rows from before 2026-10-04 were grouped per organization + user + second by the `AuditOperationBackfill` data fix.
- **Reading it:** `GET /api/activity` (`Services/Activity`): `ActivityQueries` filters and pages operations in SQL; `ActivityLookups` resolves names/numbers/amounts with one query per entity type per page; `ActivityAssembler` merges a record's rows within an operation into its net change, picks the primary record (`ActivityCatalog.RankOf`) and composes the `ActivityKind`. A new audited entity adds its kind to `ActivityCatalog` and `ActivityEntityKind`, and its label to `ActivityAssembler`. Contract: `Ombor.Docs/backend-contracts/activity.md`.
- **Tests run with the interceptor:** the integration host adds it back after replacing the DbContext options, so API writes in tests record audit rows exactly as in production.

## Multi-tenancy

Global query filter applied **by reflection over every `IOrganizationScoped` entity**, plus organization stamping on save (organization from the JWT claim). A new tenant-scoped entity implements `IOrganizationScoped` and gets both for free — **never a hand-written per-endpoint filter** (hard rule 5 / rule 34). Cross-organization isolation is covered by integration tests as a first-class invariant.

## Money & stock patterns

- **Everything money/stock is server-computed and served** (rules 8, 12): balances, totals, ages, counts, running balances, WAC. The client never derives them.
- **Balances are derived at read time — nothing is persisted.** `PartnerBalance` is a **SQL view** over the event tables; any query needing a balance joins and aggregates the view (`Partner.Balance` is removed). No stored balance columns anywhere — canon's "computed per read, never stored" (rules 12, 15) holds literally. ⚠ Mechanism corrected from owner input 2026-07-13 — the redesign plan's "persisted projection recomputed in-transaction" wording did not match what shipped; the verification pass should confirm the view mapping and how wallet balances derive.
- **Ledger is derive-on-read** — no `PartnerLedgerEntry` table. The ledger's newest-first running balance must terminate **exactly** at the served partner balance; that reconciliation is what makes a disputed figure defensible.
- **Sign convention:** `balance > 0` = the partner owes us (receivable); `< 0` = we owe them. Consistent everywhere — Partner, Order `customerBalance`, Debt direction, Dashboard.
- **WAC engine:** stock-in events recompute the average (`(oldQty·oldWAC + inQty·inCost)/(oldQty+inQty)`; a Supply enters at its net line cost (after the discount); a SaleRefund at its original sale line's cost; transfer-in carries the source WAC; Increase adjustments restore at current WAC without changing it); stock-out events leave at current WAC (Sale → COGS; Decrease adjustment → a loss line distinct from COGS). Base-unit movement with package count retained on lines.
- **Cost snapshot (added 2026-10-04, scope-9):** `MoveStockAsync` returns the unit cost each product moved at, and every transaction line stores it (`TransactionLine.UnitCost`) in the same write — through `TransactionStock.MoveAsync`, shared by transaction create and order delivery. A new path that creates sale or purchase lines goes through it; never stamp a cost from a later WAC read. A cost is never re-derived: profit for a past period must not move when prices change. Lines from before the snapshot carry a backfilled estimate with `CostIsEstimated` (`TransactionLineCostBackfill`); every figure built on one serves a `costIsEstimated` flag.
- **Negative stock is impossible** (rule 20): sale lines, decrease adjustments, transfer lines, order delivery — hard-blocked with **400 `ValidationProblemDetails`** carrying available-vs-requested detail.
- **Settlement direction:** a payment settles only transactions of its own direction (Income → Sale/SupplyRefund, Expense → Supply/SaleRefund — `PaymentDirection.SettlableTypes()`), on both create paths; the same set drives the rule-40 advance gate.
- **Refunds** are booked to the original's partner and priced from the original line (`TransactionCreateGuard.ApplyOriginalPricing`) — never from the request — so a refund cannot pay out more than was charged. Line quantities (transaction, transfer) use the quantity precision (18,3), like stock.

## Concurrency — the organization write lock (added 2026-10-04, backend-7)

Two cashiers pressing «Провести» at once must never both sell the last unit, both spend the same cash, or both settle the same debt. The rule:

- **Every money or stock write runs inside `IOrganizationWriteLock.BeginOrgWriteAsync()`**: transactions (sale, supply, both refunds), payments (incl. payroll from either path), stock adjustments, warehouse transfers, opening stock, order edits / status changes / delivery (→ Sale), wallet transfers, and the partner and wallet creates that record an opening balance. The pattern, right after the request validator:
  ```csharp
  await using var write = await writeLock.BeginOrgWriteAsync();
  // ownership checks, every read a check depends on, the writes, SaveChangesAsync
  await write.CommitAsync();
  ```
  The helper begins the transaction and takes `sp_getapplock` on `ombor:org:{id}:writes` (exclusive, owner = transaction), so one organization's writes run one at a time and the lock is released by the commit or rollback. Disposing without a commit rolls back — no try/catch is needed. Do not open a second transaction inside it, and never start a money/stock write with a bare `context.Database.BeginTransactionAsync()`.
- **Every read a check depends on happens after the lock is held**: stock on hand (`MoveStockAsync`), wallet balance (`EnsureWalletCanCoverAsync`, `ComputeWalletBalanceAsync`), remaining debt and the advance gate, the refund caps, an order's status, «not stocked yet». Load the entities the write changes after the lock too — EF hands back an already-tracked instance unchanged, so a `TransactionRecord` loaded before the lock would carry a stale `TotalPaid` into the write.
- **Timeout → 409 `conflict.busy`** (`WriteLockSettings:TimeoutSeconds`, default 10, kept under the 30 s command timeout); nothing was recorded and the client may resend. File uploads inside the write hold the lock while they are processed — fine at small-shop volume.
- **Not locked:** reads (GETs never wait), other organizations, order create (numbering has its own row lock), and master-data edits / archive / reference-gated deletes (no money or stock figure to protect; the Restrict foreign keys catch a delete racing a new reference).
- **Tests:** `tests/Ombor.Tests.Integration/Endpoints/Concurrency` fires the same write 10 times at once and asserts the invariant (stock never below zero, no overdraft, `TotalPaid` = sum of its settling allocations, one sale per order). A new money or stock write path takes the lock and adds its case there.

## Time & calendar (added 2026-10-04)

- Store and serve timestamps in UTC; answer every **calendar** question through `IBusinessClock` (`Today`, `DateOf`, `StartOfDay`, `ToLocal`) — Asia/Tashkent, UTC+5. Never `DateTime.UtcNow.Date` / `DateOnly.FromDateTime(DateTime.UtcNow)` for «today», due-date overdue, ages, opening dates or dashboard buckets: the UTC day starts at 05:00 local. `IBusinessClock.UtcNow` is the testable «now» (`TimeProvider`); `BusinessTimeZone.Tashkent` is the zone for static mappings. Tests use `Tests.Common/Helpers/BusinessDay` and `FixedTimeProvider`.

## Read models

- **Debts** has two read models. `GET /api/debts` lists unpaid documents (`remaining = total − paid`, direction from the transaction type) — document totals, not debt. **Debt** is the net partner position (business-rules «Debt totals»): computed only in `DebtPositionCalculator` (Application/Services/DebtPositions) from the `PartnerBalance` view, and served by `GET /api/debts/summary` and the dashboard. A new screen that shows «Нам должны / Мы должны» reads one of those two — never sums `/api/debts` or partner balances on its own.
- **Dashboard** debt figures come from the same calculator, so Partners, Debts and Dashboard agree by construction. Trends walk today's figures back by undoing events dated at or after each bucket end, so the last point equals the headline. `period` drives revenue (net of sale refunds), refunds, the series and the trends; values of positions (debt, cash, stock value) are today's.
- **Reports (added 2026-10-04, scope-8)** live in `Services/Reports`, one builder per report behind `IReportService`; contract `Ombor.Docs/backend-contracts/reports.md`. Periods are local days resolved once by `ReportRange` (defaults, max length); time buckets come from `ReportBuckets` (weeks keyed by Monday). Line-based figures (sales, purchases, profit, the dashboard's gross profit) all read `ReportLines` and aggregate with `LineFigures`, so every grouping and the dashboard agree; a document's sub-cent rounding residue sits on its first line so totals equal Σ `TotalDue`. Expense and cash figures count wallet-sourced components only. A new report reuses these loaders instead of summing documents its own way.
- **Two different «overdue» measures, both kept:** the dashboard's «Долги старше 30 дней» = the part of the net receivable aged 31+ days (aging attributes it to the newest items — offsets settle the oldest first); a document's `Overdue` status / `overdueDays` = past its due date. Do not unify them.

## Immutability enforcement

No PUT/DELETE surface — at endpoint **and** service level — for TransactionRecord, Payment, PaymentComponent, PaymentAllocation, Payroll, StockAdjustment, Transfer (hard rule 1). Corrections are counter-events. Opening balances and opening stock are auditable events, never raw field assignments (rule 16, 22).

## Contract & endpoint conventions

- Transaction creation is a **single `POST /api/transactions`** with the `type` discriminator (Sale / Supply / SaleRefund / SupplyRefund) — **no separate `/refund` endpoint** (settled). The request is **`multipart/form-data`** (a `payload` JSON part + attachment file parts), since transactions carry attachments. ⚠ The frontend mock still exposes `POST /{id}/refund` — reconciliation tracked in the delta queues.
- PUT semantics for nullable references follow the Order-warehouse precedent: **`undefined` = keep existing, `null` = explicit clear, value = set.**
- Reference-gated DELETE (Partner, Category) returns 409 while referenced; `isDeletable` is served and must agree with the guard.

## Testing

- **Heavy integration tests on the money/stock invariants** — WAC, source = settling allocations, atomicity, negative-stock blocks, organization isolation — via Testcontainers SQL Server 2022 (Docker/Podman required). **Thin, focused unit tests** elsewhere.
- Setup through `Ombor.TestDataGenerator`; never bespoke seed paths. **Seeding by environment:** Development and Testing seed demo data; Production and Staging seed **nothing** (`NoDataSeeder`) — a live host never gains demo organizations, users with a known password, or fabricated payments. Startup still applies migrations everywhere. Every seed step runs once (skips when its rows exist) — payments included, so a restart never fabricates payments against documents made in the app; the Development seed finishes each demo organization with registration's starter rows through `IOrganizationSetupService` (idempotent by name).
- **Data fixes** (re-deriving stored values, converting legacy formats) are idempotent SQL kept as a constant in `Infrastructure/Persistence/DataFixes`, run by a migration and executed by an integration test against planted bad rows — dry-run the statement on the local DB first.
- **Run the full suite as one batch** — shared-context bleed can hide failures an isolated run never sees (the `EndpointTestsBase` shared-DbContext lesson). Assert the behavior actually changed, not merely that the build is green.

## Docs & comments

XML docs on DTOs and public classes. Comments only where they explain a **decision, reason, or tricky part** — never narrating what the code does. All code, comments, and commits in English.
