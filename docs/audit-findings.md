# Ombor Backend — Audit Findings

**Session:** First backend recon (read-only). No code, migrations, or files changed except this doc.
**Date:** 2026-06-18
**Scope:** Verify mechanical facts, trace one endpoint end-to-end, verify the `backend-contract.md` status labels against real code, and surface conflicts/risks for the build.
**Source of truth for "current state":** the code at `src/` on branch `redesign/claude-docs`. Where the contract or complexity docs disagree with code, the code wins and the doc is flagged.

> ⚠️ **Headline correction up front:** the backend is **much further along than `backend-complexity-notes.md` assumes.** Multi-tenancy (global query filter + tenant stamping), the audit interceptor, partial WAC/stock movement, refund-rule validation, and the Payment/Transaction schema all already exist. But almost all of it is built on the **legacy payment model** (`PaymentMethod`/`Currency`/`ExchangeRate`, stored `PartnerBalance`), which is exactly the model the redesigned contract and `business-rules.md` say to drop. The build is therefore less "greenfield" and more "migrate a substantial legacy implementation to the canon."

---

## Part 1 — Mechanical facts

### Platform & versions (read from repo)

| Fact | Value | Source |
| --- | --- | --- |
| .NET SDK | **8.0.100**, `rollForward: latestMinor` | [global.json](global.json) |
| Target framework | **net8.0** (all projects) | all `*.csproj` |
| EF Core | **8.0.20** (`Microsoft.EntityFrameworkCore`, `.SqlServer`, `.Tools`); Design **8.0.16** in API | [Ombor.Infrastructure.csproj](src/Ombor.Infrastructure/Ombor.Infrastructure.csproj), [Ombor.API.csproj](src/Ombor.API/Ombor.API.csproj) |
| Database | **SQL Server** via `UseSqlServer(...)`. Connection key `ConnectionStrings:DefaultConnection` | [Infrastructure/Extensions/DependencyInjection.cs:29-32](src/Ombor.Infrastructure/Extensions/DependencyInjection.cs) |
| Validation | **FluentValidation** 11.3.0 (`FluentValidation.AspNetCore`) | [Ombor.Application.csproj](src/Ombor.Application/Ombor.Application.csproj) |
| Mapping | **Manual** (hand-written extension methods in `Application/Mappings`). No AutoMapper/Mapster present. | [Application/Mappings/](src/Ombor.Application/Mappings) |
| API versioning | `Asp.Versioning.Mvc` 8.1.0 is referenced **but no controller uses `[ApiVersion]`** — routes are unversioned `api/<resource>`. | controllers |
| Other infra | JWT bearer auth; Swashbuckle 8.1.1 (+ Newtonsoft support); Sentry (prod only); SixLabors.ImageSharp for thumbnails; StackExchange.Redis referenced **but `IRedisService` is wired to an in-memory `MemoryCache`** | [Infrastructure DI:37-43](src/Ombor.Infrastructure/Extensions/DependencyInjection.cs) |

**Solution layout** (`Ombor.sln`): `Ombor.API` (web) → `Ombor.Infrastructure` (EF, persistence, external services) → `Ombor.Application` (services, validators, mappings, interfaces) → `Ombor.Contracts` (request/response DTOs) + `Ombor.Domain` (entities, enums). Plus `Ombor.TestDataGenerator` (Bogus seed data) and three test projects.

### Commands

| Action | Command |
| --- | --- |
| Build | `dotnet build Ombor.sln` |
| Run locally | `dotnet run --project src/Ombor.API` (profiles `http` → `http://localhost:5062`, `https` → `https://localhost:7015`). Swagger UI is the launch URL. |
| Test (all) | `dotnet test Ombor.sln` |
| Test (unit only) | `dotnet test tests/Ombor.Tests.Unit` |
| Test (integration only) | `dotnet test tests/Ombor.Tests.Integration` |
| Coverage | runsettings present: `coverlet.runsettings` (excludes migrations + TestDataGenerator) |
| Add migration | `dotnet ef migrations add <Name> --project src/Ombor.Infrastructure --startup-project src/Ombor.API` |
| Apply migration | Not normally needed manually — **startup runs `context.Database.MigrateAsync()`** automatically ([StartupExtensions.cs:18](src/Ombor.API/Extensions/StartupExtensions.cs)). Manual: `dotnet ef database update --project src/Ombor.Infrastructure --startup-project src/Ombor.API`. |

Migrations live in `src/Ombor.Infrastructure/Persistence/Migrations`; no explicit `MigrationsAssembly` is set, so they default to the Infrastructure assembly (where the DbContext lives). This is correct and conventional.

### Integration-test database provisioning

- **Testcontainers (`Testcontainers.MsSql` 4.4.0)** spins up a real SQL Server 2022 container per test run — **not** LocalDB, not in-memory. `MsSqlBuilder().WithCleanUp(true).Build()`, `StartAsync()`, then `EnsureDeletedAsync()` + `MigrateAsync()` against catalog `TestDB`. See `tests/Ombor.Tests.Integration/Helpers/DatabaseFixture.cs`.
- A `TestingWebApplicationFactory : WebApplicationFactory<Program>` swaps the DbContext to the container connection string and installs a fake "Test" auth scheme; environment set to `Testing`.
- xUnit collection fixture (`[Collection(nameof(DatabaseCollection))]`) shares one container across the integration suite.
- **Implication for local runs/CI: Docker must be available** for integration tests. Unit tests (`Ombor.Tests.Unit`, xUnit + Moq/AutoFixture + MockQueryable) need nothing external.

### Local run model

**Run-on-demand**, not a long-running daemon to assume. `dotnet run` migrates the DB and then seeds it via `Ombor.TestDataGenerator` (environment-specific seeder; dev seeds large fake datasets — 1000 products etc.). The `EnsureDeletedAsync()` line is commented out, so the dev DB persists across restarts. A `docker-compose.yaml` + `Dockerfile` exist for containerized run (API + SQL Server 2022).

### Configuration & secrets

- Layered `appsettings.json` + `appsettings.{Environment}.json` (Development/Staging/Production/Testing). Keys: `ConnectionStrings` (`DefaultConnection`, `Redis`), `Jwt`, `Sms` (Eskiz), `FileSettings`, `Cors:AllowedOrigins`, `Cookie*`, `DataSeedSettings`, `Swagger`, `Sentry` (prod).
- **No `dotnet user-secrets`** (no `UserSecretsId`). **No environment-variable secret layering** except `docker-compose` passing `ConnectionStrings__DefaultConnection`.
- Strongly-typed options (`JwtSettings`, `SmsSettings`, `FileSettings`, `CookieSettings`) are bound with `ValidateDataAnnotations().ValidateOnStart()` ([Application DI:53-75](src/Ombor.Application/Extensions/DependencyInjection.cs)).
- 🔴 **Security finding (see Part 4):** real-looking **production DB credentials and SMS/JWT keys are committed** in `appsettings.Production.json`. Flagged, not touched.

### Migration-policy recommendation

Code **can safely draft and apply migrations locally** — the dev DB is a disposable seeded SQL Server, startup auto-migrates, and integration tests run on throwaway containers. **Recommended policy:** Code may **author** migrations and **apply them to the local/dev DB**, but should treat **schema changes as review-gated** — every migration in this redesign is consequential (multi-tenancy, payment-model rework). **Never** run destructive operations against the production connection string, and never auto-apply to prod from a dev session. Net: *draft + apply-locally allowed; production apply is human-gated.*

---

## Part 2 — Architecture trace (seed for `backend-conventions.md`)

Traced **Categories** (`GET/POST/PUT/DELETE /api/categories`) end-to-end; the same shape repeats across Products, Partners, Inventories, Templates, Employees.

### Layering & flow

```
Controller (Ombor.API)  →  Service (Ombor.Application)  →  IApplicationDbContext (EF)  →  manual Mapping ext.  →  DTO (Ombor.Contracts)
        ▲ FluentValidation via IRequestValidator ▲              ▲ global query filter + audit interceptor ▲
```

### Controller conventions ([CategoriesController.cs](src/Ombor.API/Controllers/CategoriesController.cs))

- `[ApiController]`, `[Authorize]`, `[Route("api/categories")]` — **lowercase, plural, kebab-free** resource routes. All controllers follow this; **no API versioning segment.**
- `sealed` class, **primary-constructor DI** (`CategoriesController(ICategoryService categoryService)`).
- Action methods are `async` with the `Async` suffix retained (`SuppressAsyncSuffixInActionNames = false`).
- Route constraints inline: `[HttpGet("{Id:int:min(1)}")]`.
- Requests bind via `[FromQuery]` / `[FromRoute]` / `[FromBody]` **request records** (even single-id reads use a `GetCategoryByIdRequest` record).
- `[ProducesResponseType(...)]` declared for each status (200/201/400/404).
- Controllers are **thin**: delegate to the service, wrap in `Ok(...)` / `CreatedAtAction(...)` / `NoContent()`. The only inline logic is the PUT route-vs-body id-mismatch guard returning a hand-built `ProblemDetails`.
- XML-doc `<summary>`/`<param>`/`<returns>` on every public action (`GenerateDocumentationFile=true`, fed to Swagger). (Several typos in doc comments, e.g. "maanage".)

### Service conventions ([CategoryService.cs](src/Ombor.Application/Services/CategoryService.cs))

- `internal sealed`, primary-constructor DI of `IApplicationDbContext` + `IRequestValidator`.
- **Validation lives in the service**, first line of each write: `await validator.ValidateAndThrowAsync(request)`. (Note: list `GetAsync` is *not* validated.)
- `IRequestValidator` resolves `IValidator<TRequest>` from DI and throws FluentValidation's `ValidationException` on failure ([RequestValidator.cs](src/Ombor.Application/Services/RequestValidator.cs)). Validators auto-registered via `AddValidatorsFromAssembly`.
- Not-found handled by a private `GetOrThrowAsync` → `throw new EntityNotFoundException<Category>(id)`.
- DTO projection done two ways: list queries project **inline** in the LINQ `Select` (`new CategoryDto(...)`, `AsNoTracking()`); single-entity reads/writes use **mapping extensions** (`entity.ToDto()`, `request.ToEntity()`, `entity.ApplyUpdate(request)`).
- Business logic is procedural inside the service; there are no domain-method-rich aggregates (entities are mostly anemic, though `TransactionRecord` has `AddPayment`, `Partner.CanHandleTransaction`).

### DTO ↔ entity mapping ([CategoryMappings.cs](src/Ombor.Application/Mappings/CategoryMappings.cs))

- **Manual**, `internal static` extension classes per entity in `Application/Mappings`. `ToDto`, `ToEntity`, `ToCreateResponse`, `ToUpdateResponse`, `ApplyUpdate`. No mapping library.

### EF / persistence

- `ApplicationDbContext : DbContext, IApplicationDbContext` ([ApplicationDbContext.cs](src/Ombor.Infrastructure/Persistence/ApplicationDbContext.cs)); the **interface lives in Application** so services never reference the concrete context.
- Entity configs: one `internal sealed XxxConfiguration : IEntityTypeConfiguration<Xxx>` per entity in `Persistence/Configurations`, auto-applied via `ApplyConfigurationsFromAssembly`. Convention: `builder.ToTable(nameof(Xxx))`, `#region Properties`, shared lengths from `ConfigurationConstants` ([CategoryConfiguration.cs](src/Ombor.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs)).
- **Global query filter** applied by reflection to every `ITenantScoped` entity (Part 4). **Tenant stamping** on `SaveChanges`.
- **Interceptors:** exactly one — `AuditSaveChangesInterceptor` ([file](src/Ombor.Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs)). It writes an `AuditEntry` (actor, timestamp, entity type+id, action, before/after JSON, tenant) for every `IAuditable` entity change, backfilling the new id for inserts in `SavedChanges`.

### Error shape (frontend depends on this)

- `ProblemDetails`/`ValidationProblemDetails` are produced by **`IExceptionHandler` implementations** (`UseExceptionHandler`), not filters or per-action code. Registered in order: `ValidationExceptionHandler` → `EntityNotFoundExceptionHandler` → `InvalidFileExceptionHandler` → `GlobalExceptionHandler` ([API DI:47-57](src/Ombor.API/Extensions/DependencyInjection.cs)).
- `ValidationExceptionHandler` → **400 `ValidationProblemDetails`** with `Errors` = `Dictionary<string,string[]>` grouped by `PropertyName` ([file](src/Ombor.API/ExceptionHandlers/ValidationExceptionHandler.cs)). Matches the contract's field-keyed `errors` shape.
- `EntityNotFoundExceptionHandler` → **404 `ProblemDetails`**; `GlobalExceptionHandler` → **500 `ProblemDetails`** (message hidden in prod).
- `ConfigureApiBehaviorOptions(SuppressModelStateInvalidFilter = true)` — the automatic MVC 400 is **disabled**; all validation flows through FluentValidation. JSON uses web defaults (camelCase properties) + `JsonStringEnumConverter` (**enums serialized as strings**) + ignore-nulls-when-writing.
- ⚠️ **Error-key casing drift:** the `errors` dictionary keys come straight from FluentValidation `PropertyName` (PascalCase, e.g. `"Name"`, `"Lines[0].Quantity"`). `DictionaryKeyPolicy` is not set to camelCase, so keys are **not** camelCased like the contract's example `lines[0].quantity`. Confirm whether the frontend matches keys case-insensitively; if not, this needs a `DictionaryKeyPolicy` or per-validator key override. (Part 4 risk.)

### Naming / folder conventions in use

- Projects = clean-ish layering: `Domain` (entities/enums/exceptions, no deps), `Application` (services/validators/mappings/interfaces, depends on Contracts+Domain), `Infrastructure` (EF + external), `API` (controllers/handlers/filters/extensions), `Contracts` (DTOs split `Requests/<Area>` and `Responses/<Area>`).
- Validators grouped per area under `Application/Validators/<Area>`, named `<Request>Validator`.
- `internal sealed` is the default for services/configs/handlers; `InternalsVisibleTo` exposes internals to the test projects.
- DI composed via per-layer `AddApi` / `AddApplication` / `AddInfrastructure` / `AddTestDataGenerator` extension methods.

---

## Part 3 — Corrected contract-status table

Verified against controllers in `src/Ombor.API/Controllers` and DTOs in `src/Ombor.Contracts`. **"Contract label"** = what `backend-contract.md` claims; **"Real state"** = what the code actually is.

| # | Resource | Contract label | Real state | Verdict / drift |
| --- | --- | --- | --- | --- |
| 1 | **Auth** | REAL (+reset MOCKED) | `login`/`register`/`verification`/`refresh-token`/`logout` exist. **No** `forgot-password` / `verify-reset-code` / `reset-password`. | ✅ Accurate. Reset trio genuinely MOCKED. |
| 2 | **Settings / Users** | MOCKED | **No SettingsController.** | ✅ Accurate — fully absent. |
| 3 | **Categories** | STALE (needs `productCount` + 409 gate) | Full CRUD exists. `CategoryDto = (Id, Name, Description)` — **no `productCount`**. **DELETE hard-deletes with no 409 gate**, and `CategoryConfiguration` sets **`OnDelete(Cascade)` Category→Products** (deleting a category would cascade-delete its products). | ✅ STALE confirmed + **worse than documented**: cascade-delete is dangerous and contradicts the reference-gate intent. |
| 4 | **Products** | STALE | CRUD + `/archive` + `/restore` + `/{id}/transactions` exist; create/update are **multipart/form-data**. `ProductDto` exposes `InventoryItems[]` + `IsLowStock`, but **still carries `QuantityInStock`** and has **no root `totalStock` / `averageCost`**. **No `/movements` endpoint.** | ✅ STALE confirmed. Gaps: `totalStock`/`averageCost` aggregates, `/movements`, removal of `QuantityInStock`. |
| 5 | **Warehouses** | STALE (live = `/api/inventories`) | Resource is **`/api/inventories`** (`InventoryDto`). Has `/opening-stock`. **No `/stock`, no `/movements`, no `/archive`/`/restore`.** Opening-stock + WAC partially implemented. | ✅ Accurate. Big gap to target `/warehouses` shape (rename, sub-resources, archive, computed totals). |
| 6 | **Stock Adjustments** | MOCKED | **No controller, no entity, no enum** (`StockAdjustmentDirection` absent). | ✅ Genuinely absent. Note: stock-out decrement logic partially exists inside the transaction path. |
| 7 | **Transfers** | MOCKED-at-target (live DTO stale) | `GET (list + by-id)` + `POST` exist. `TransferDto` carries `Status` and `FromInventoryId/ToInventoryId` naming; **no `createdBy`, no unit**. | ✅ Accurate — live DTO is stale vs target (`fromWarehouseId`, `createdBy`, no status). |
| 8 | **Partners** | STALE | CRUD + `/archive` + `/restore` + `/{id}/payments` exist. `PartnerDto` has `Balance` + legacy `BalanceDto` (`PartnerBalance`), but **no `openingBalance`, `isDeletable`, `activityCount`**; **no `/ledger`**; **DELETE has no 409 gate** (plain 204/404). | ✅ STALE confirmed. Balance comes from a **stored** `PartnerBalance` table (see Part 4), not computed. |
| 9 | **Wallets (Касса)** | MOCKED | **No Wallet entity, no controller, no `WalletType` enum.** | ✅ Genuinely absent — the single largest missing foundation (payments reference `walletId` that has nowhere to point). |
| 10 | **Payments** | MOCKED-at-redesign (live = legacy) | `GET (list)` + `POST` exist; **`GET /{id}` throws `NotImplementedException`**. **No `/form-data`, no `/outstanding`.** DTOs are the **legacy model**: `CreatePaymentRequest(Amount, ExchangeRate, Currency, Method)`; `PaymentComponent` carries `ExchangeRate/Currency/PaymentMethod`; `PaymentAllocationType = {Sale,Supply,SaleRefund,SupplyRefund,AdvancePayment,ChangeReturn}`. **No `PaymentSourceType` (Wallet/Advance) enum at all.** | ✅ Accurate that the redesigned model must be built — but note **a legacy Payment implementation already exists and is load-bearing** for Transactions, so this is a *migration*, not a clean build. |
| 11 | **Transactions** | STALE/partial | `GET (list)` + `GET /{id}/payments` + `GET /{id}/lines` + **`POST` (multipart only)** exist. **No `/{id}/refund` endpoint** — but refund creation + rules 2-6 are handled **inside `POST /api/transactions`** when `Type=SaleRefund/SupplyRefund` + `OriginalTransactionId`. POST already moves stock, recomputes WAC on Supply, hard-blocks negative stock, applies legacy payment, all inside a DB transaction. | ⚠️ **Doc drift:** contract wants JSON POST + a dedicated `/refund`; code has **multipart POST that also does refunds**. Logic is richer than "partial" implies, but bound to the legacy payment/balance model. |
| 12 | **Orders** | STALE | `GET (list/by-id)` + `POST` + state transitions `process/ship/deliver/cancel/reject/return` (split across `OrdersController` and `OrderStatesController`, both on `api/orders`). **`DeliverOrderRequest` has only `OrderId` — no `warehouseId`.** `OrderDto` lacks `saleId`, `history`, `deliveryDate`/`deliveryTime`, per-line `sku`; uses an `AddressDto` for delivery. Deliver does **not** promote to a Sale. | ✅ STALE confirmed. Gaps: deliver-time warehouse + stock check + Sale promotion, status history, date/time split, sale link. |
| 13 | **Templates** | STALE | Full CRUD. `TemplateItem` has `Discount` only (**percentage, no `discountType`**), no `sku`/`measurement`/`lastUsedAt`. **DELETE has no reference gate** (correct per contract — it's just a basket). | ✅ STALE confirmed + the discount-type gap the contract flags. |
| 14 | **Employees / Payroll** | REAL (payroll needs canon) | Employee CRUD REAL. `GET/POST /{id}/payrolls` exist and route payroll **through `IPaymentService`** (so payroll = a Payment). **No individual `GET/PUT/DELETE /payrolls/{paymentId}`** found — so the "payroll PUT/DELETE to remove" may already be gone (the contract's claim that they exist "today" looks **stale**). `DELETE /employees/{id}` exists (UI doesn't call it). | ⚠️ Contract over-states what exists: per-payroll PUT/DELETE endpoints were **not found**. Verify, then update both docs. Payroll create still uses **legacy `CreatePayrollRequest`** (currency/method/exchangeRate). |
| 15 | **Debts** | MOCKED | **No controller / no derivation.** | ✅ Genuinely absent. |
| 16 | **Dashboard** | MOCKED | **No controller.** | ✅ Genuinely absent. |

**Net:** every MOCKED label (Settings, Wallets, Stock Adjustments, Debts, Dashboard, password-reset) is **accurate**. STALE labels are accurate but two are *understated* — Categories (dangerous cascade-delete) and Transactions (refund logic + WAC already live in the create path). The contract is **wrong/over-stated** about Employees payroll PUT/DELETE existing "today" (not found) and **mislabels the Payment direction**: it isn't a clean MOCKED build, it's a legacy implementation that must be reworked while Transactions depend on it.

---

## Part 4 — Conflicts & risks

### 4.1 Contract-doc / business-rules / code three-way conflicts

| Topic | business-rules.md | backend-contract.md | Code today | Conflict |
| --- | --- | --- | --- | --- |
| **Tenant vs Organization** | Backend entity is **`Organization`**, keyed `OrganizationId` (rule 34, domain model). | Internal entity **stays `Tenant`/`TenantId`**; only the API field is `organizationName` (decision 4.1). | Entity is **`Tenant`** + `TenantId` everywhere; tenancy resolved from JWT claim `tenant_id`. No `organizationName` surfaced yet (Settings unbuilt). | **business-rules says `Organization`, code+contract say `Tenant`.** Pick one naming canon. Code matches the contract, not the rules doc. |
| **System partner / `partnerId`** | Rule 39: every sale needs an explicit partner; `partnerId` required non-null; starter partner is ordinary (rule 42). | Decision 4.2: seeded ordinary defaults, no `isSystem`, `partnerId` required. | `CreateTransactionRequest.PartnerId` is **non-nullable `int`** ✅. **But there is no rule-42 org-setup seeding** (1 cash wallet / warehouse / category / partner) — the only seeder is the dev/test Bogus bulk generator, and **Wallet doesn't exist to seed.** | Aligned in principle; **org-provisioning seeding is unbuilt** and partly un-buildable (no Wallet entity). |
| **Fixed vs percentage discount** | Rule 37: line-level, `discount`+`discountType` persisted, `Fixed` = currency off whole line. | Decision 4.4: persist `discount`+`discountType`. | `TransactionLine` has **`Discount` only, hard-coded percentage** (`Total = UnitPrice*Qty*(1-Discount/100)`); `TemplateItem` same. **No `DiscountType` anywhere.** | **Code violates the canon.** Needs `DiscountType` column + recompute logic on TransactionLine, OrderLine, TemplateItem. |
| **Payroll immutability** | Rule 1: payroll immutable, any number per month, redesigned payment model, no method/currency/rate. | §14: remove payroll PUT/DELETE, drop one-per-month, move to redesigned model. | No per-payroll PUT/DELETE found (likely already removed). Payroll **create still legacy** (`CreatePayrollRequest` currency/method/exchangeRate) and flows through legacy `PaymentService`. | Mostly aligned on immutability; **still on the legacy payment model.** |
| **Balances computed vs stored** | Rules 12, 15; complexity §A2: **never store balances**, compute from the event ledger. | §0 "⚙ computed" everywhere; complexity §A2. | **`PartnerBalance` is a stored entity/table** (`PayableDebt`, `ReceivableDebt`, `PartnerAdvance`, `CompanyAdvance`) read directly in `TransactionService.ValidateOrThrowAsync`. `Product.QuantityInStock` still stored. | **Direct violation of the core "computed, never stored" principle.** This is the deepest architectural conflict. |
| **Payment allocation model** | Rules 8-11: source `{Wallet,Advance}` = settling allocations `{TransactionSettlement,AdvanceCredit}`; `ChangeReturn` is a memo. | §10 redesigned model. | Legacy: no `PaymentSourceType`; `PaymentAllocationType = {Sale,Supply,SaleRefund,SupplyRefund,AdvancePayment,ChangeReturn}`; components carry currency/rate/method. | **Entire payment model must be re-modeled.** |

Also: **complexity notes §A1 is stale** — it says "today only `User`/`Role` carry `OrganizationId`." In reality 22 entities implement `ITenantScoped` and a global filter is enforced. And **§B's "all WAC is unimplemented today" is wrong** — WAC recompute on Supply, refund re-entry, and negative-stock blocking already exist in `TransactionService.UpdateProducts`, with `AverageCost` logic also touched in `TransferService` and `InventoryService`.

### 4.2 Highest-risk items for the build

1. **Multi-tenancy scoping — LOWER risk than documented (already built, verify completeness).**
   A real global query filter exists: `ApplicationDbContext.OnModelCreating` reflects over every `ITenantScoped` and calls `HasQueryFilter(e => CurrentTenantId == 0 || e.TenantId == CurrentTenantId)`, plus `StampTenant()` on save and a `TenantId` index. Tenant comes from JWT claim `tenant_id`. **Residual risks:** (a) the `CurrentTenantId == 0` escape hatch bypasses the filter entirely (used for seeding/design-time) — make sure no production code path runs with tenant 0; (b) any new entity that forgets `ITenantScoped` silently skips scoping; (c) `PartnerBalance` is **not** `ITenantScoped` (not in the 22) — a hole if it's kept. Confirm cross-tenant isolation with an explicit integration test.

2. **Migrating the legacy Payment model to source/allocation — HIGH risk.**
   The redesigned model (sources `{Wallet,Advance}`, allocations `{TransactionSettlement,AdvanceCredit,ChangeReturn}`, the rule-8 balance identity, advance-as-claim, change-return-as-memo) is **the heart of the product** and is **not** what's coded. Because `TransactionService` already consumes the legacy `PaymentService`, `PaymentMethod.AccountBalance`, `ExchangeRate`, and stored `PartnerBalance`, this rework ripples through transaction creation, refunds, and payroll simultaneously. **Sequence it deliberately** (Wallets → redesigned Payment → re-point Transactions).

3. **Wallets are completely missing — HIGH risk / foundational blocker.**
   No entity, table, controller, or enum. Payments, "our money", advances held, wallet transfers, and the dashboard all depend on it. Nothing downstream of payments can be built correctly until Wallet exists. This is the true critical-path item, ahead of even the payment rework.

4. **WAC engine — MEDIUM (partially built, but partial = dangerous).**
   Supply stock-in recompute, sale-refund re-entry, and negative-stock blocking exist for the **transaction** path inside one DB transaction. **But** opening-stock, transfers, and (future) stock-adjustments each touch `AverageCost` in separate services — consistency across all five stock-in/out event types is unverified, and `Product.QuantityInStock` still exists as a parallel, writable stock figure that can drift from `InventoryItem`. Risk is *inconsistency*, not *absence*.

5. **Transaction-creation atomicity — MEDIUM (looks correct, audit it).**
   `TransactionService.CreateAsync` wraps stock + transaction + payment in an explicit `BeginTransactionAsync`/`Commit`/`Rollback`. That's the right shape. Verify the audit interceptor's **`SavedChanges` second `SaveChanges()`** (id-backfill) participates in the same transaction and can't half-commit, and that nested `PaymentService.CreateAsync` shares the connection/transaction.

6. **Balance correctness (stored `PartnerBalance`) — HIGH risk to the product's core promise.**
   The dispute-grade "every som traceable from the ledger" guarantee is impossible while balances are stored and mutated. Reworking to computed/ledger-derived balances is large and touches partners, wallets, debts, dashboard.

### 4.3 Anything surprising

- 🔴 **Committed production secrets.** `appsettings.Production.json` contains a real-looking SQL Server connection string with username/password, plus a Sentry DSN; `docker-compose.yaml` has a plaintext SA password; JWT signing key and Eskiz SMS token are committed and **shared across environments**. **Recommend rotating these and moving to user-secrets/env vars** regardless of the redesign. (Flagged only — not modified.)
- **`TransactionType.WriteOff = 5` still exists** in the enum and is referenced in a `TransactionService` stock-out comment, even though business-rules replaced WriteOff with StockAdjustment. Dead/contradictory enum value.
- **`Product.QuantityInStock` is still a writable column** (`Product.cs:38`) despite rule 17 / the contract's "remove it" note. Surfaced in `ProductDto`. Schema oddity + drift risk.
- **Two controllers on one route:** `OrdersController` and `OrderStatesController` both declare `[Route("api/orders")]`. Works via action/verb routing but is a discoverability/ambiguity smell.
- **Category → Products is `OnDelete(Cascade)`** — a category delete would silently delete its products. Directly opposed to the intended 409 reference-gate.
- **`GET /api/payments/{id}` throws `NotImplementedException`** — a live endpoint that will 500.
- **Redis is referenced but not used** — `IRedisService` resolves to an in-memory `MemoryCache` singleton; the `Redis` connection string is decorative today.
- **`PartnerBalance` legacy entity** is stored, not tenant-scoped, and exposed via `PartnerDto.BalanceDto` (the contract calls this field "legacy, unused by redesign") — but it's *actively used* by `TransactionService` validation, so it's not dead yet.
- **List endpoints already return full arrays** (no pagination) — matches the v1 decision; client-side filtering assumption holds.
- **`RequireHttpsMetadata = true`** on JWT bearer even in dev; integration tests bypass via a fake "Test" auth scheme.

---

## Decisions needed before implementation

1. **Naming canon: `Tenant` or `Organization`?** Code + contract say keep internal `Tenant`/`TenantId` and expose `organizationName`. business-rules.md says the entity *is* `Organization`. Confirm we keep `Tenant` internally (lowest churn) and fix the rules doc — or commit to a rename.
2. **Build order for the payment rework.** Confirm the critical path: **Wallet entity → redesigned Payment (source/allocation) → re-point Transactions/Refunds/Payroll off the legacy model → delete `PaymentMethod`/`ExchangeRate`/`Currency` + legacy `PaymentAllocationType` values.** Is a clean cut acceptable (drop legacy outright) or is a transitional period required?
3. **Balances: computed vs stored.** Do we commit now to removing `PartnerBalance` and computing balances from a ledger (true to canon, large effort), or keep a reconciled projection short-term? This decision gates Partners, Wallets, Debts, and Dashboard.
4. **`Product.QuantityInStock` removal timing.** Drop the column now (migration) or after the WAC paths are fully consolidated? Anything still reading it must move to `InventoryItem` first.
5. **Categories delete semantics.** Change `OnDelete(Cascade)` → `Restrict` + a 409 reference-gate (and add `productCount`)? Confirm this is in-scope for the Categories pass.
6. **Refund endpoint shape.** Keep refunds inside `POST /api/transactions` (current code) or split out the contract's dedicated `POST /api/transactions/{id}/refund` + JSON create? Frontend calls the latter.
7. **Error-key casing.** Does the frontend match `errors` keys case-insensitively? If not, we must camelCase dictionary keys (and emit `lines[0].quantity`-style indexed keys) — confirm the exact expected format with a real frontend sample.
8. **Migration apply policy.** Confirm the recommended policy: Code may draft migrations and apply to local/dev, but production apply stays human-gated. Confirm Docker is available in CI for the Testcontainers integration suite.
9. **Secret remediation.** Out of scope for the redesign, but confirm you'll rotate the committed prod DB/JWT/SMS/Sentry secrets and move them to user-secrets/env vars.

---

*Next session input: this doc feeds finalizing `CLAUDE.md` and authoring `backend-conventions.md`. No implementation, migrations, or refactors were performed.*
