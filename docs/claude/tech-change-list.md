# Tech change list

Living document. Tracks gaps between current code state and target state defined by `rules.md` and `claude-context.md`.

**Status:** not started / in progress / complete / blocked
**Severity:** blocker (cannot ship MVP without) / important / nice-to-have

Phase 1 audit will confirm and extend this list. Items below are from initial schema and OpenAPI review.

---

## Multi-tenancy enforcement

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: the `Organization` concept was renamed to `Tenant` end-to-end (entity, service,
DbSet, FK columns — `OrganizationId` → `TenantId`). Every tenant-scoped entity now
implements `ITenantScoped` and carries a `TenantId` column + index. `ApplicationDbContext`
applies an EF Core global query filter to every `ITenantScoped` type and stamps
`TenantId` on insert. The tenant is resolved from a `tenant_id` JWT claim via
`ITenantAccessor` (`HttpContextTenantAccessor`); `JwtTokenService` emits the claim.
Migration: `Add_Multi_Tenancy`. Seeding pins a tenant through `ITenantAccessor.SetTenant`.

## Weighted-average cost on InventoryItem

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: `AverageCost` field on InventoryItem; `TransactionService.UpdateProducts` recomputes
it on every Supply stock-in per the rules.md #10 formula. SaleRefund re-enters stock at
the existing carrying cost (no change).

## WriteOff transaction type

**Status:** enum + stock handling done (2026-05-17); write path deferred
**Severity:** blocker

Done: `WriteOff` enum value; `TransactionService.UpdateProducts` treats it as a stock-out.
Deferred: the create path still requires a partner. WriteOff has no partner, so
`TransactionRecord.PartnerId` / `CreateTransactionRequest.PartnerId` must become nullable
(schema migration + mapper/query null-handling) before WriteOff is reachable. Pairs well
with the Warehouses/transfers work (Phase 3 items #4-5).

## Inter-warehouse transfers

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: `TransferService` + `TransfersController` (`GET`, `GET /{id}`, `POST /api/transfers`).
`CreateAsync` moves stock atomically (single SaveChanges) — decrements the source
`InventoryItem`, increments the destination, hard-blocks negative source stock, and carries
the weighted-average cost with the goods. Transfers, transfer lines and the inventory-item
changes are all audited via the audit interceptor.

## Transaction-to-warehouse linkage

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: `InventoryId` FK on TransactionRecord; `TransactionService` requires it at write
time, validates the warehouse exists, and applies stock movement to that warehouse's
`InventoryItem` rows.

## Refund linking

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: `OriginalTransactionId` FK on TransactionRecord; `TransactionService.ValidateRefundOrThrowAsync`
enforces rules.md #2-6 — required for refund types, type must match the original, no
refund-of-refund, and total refunded quantity per product capped to the original.

## Archive mechanism

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: archive is a distinct operation from deletion — the consumer chooses. Product and
Partner each expose three operations:
- `DELETE /{id}` — hard delete; refused with a clear error if the entity is referenced by
  transaction/order/payment history (rules.md #16).
- `POST /{id}/archive` — sets the `IsArchived` flag.
- `POST /{id}/restore` — clears it.

List endpoints take an `isArchived` query flag (default lists active; `true` lists archived
so the frontend can offer a restore view). Archived rows are filtered only from list
endpoints, not globally — transaction/order history must still resolve the entity.
The flag is named `IsArchived` (not `IsDeleted`).

## Audit log

**Status:** complete (2026-05-17)
**Severity:** blocker

Done: `AuditEntry` table + `AuditSaveChangesInterceptor` (registered on the DbContext)
records every insert/update/delete of an `IAuditable` entity — TransactionRecord, Payment,
PaymentComponent, PaymentAllocation, InventoryItem, Transfer, TransferLine — with actor
(`ICurrentUserAccessor`), timestamp, before/after JSON, entity type and id. Insert ids are
back-filled after the row is written.

## Opening balance and opening stock as events

**Status:** opening stock done (2026-05-17); opening balance pending
**Severity:** important

Done: opening stock — `POST /api/inventories/{id}/opening-stock` creates `InventoryItem`
rows with their initial weighted-average cost; the inserts are recorded by the audit
interceptor (actor + timestamp), so opening stock is an auditable event.
Pending: partner opening balance is still a raw field on partner creation — it needs the
same auditable-event treatment.

## Cyrillic↔Latin search

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: not implemented.
Target: search across name fields handles both scripts transparently.

## Server-side pagination, filtering, search

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: not implemented.
Target: list endpoints support paging, filtering, search; client doesn't load full tables.

## Product.QuantityInStock removal

**Status:** in progress (2026-05-17)
**Severity:** important

Done: `TransactionService` no longer reads or writes `Product.QuantityInStock` — stock now
flows entirely through `InventoryItem`. The dual-source-of-truth bug is closed.
Pending: the `QuantityInStock` field/column still exists and is surfaced in product DTOs,
validators and the test data generators. Full removal (DTO/contract/migration cleanup) is
the remaining step.

## Payroll immutability

**Status:** complete (2026-05-17)
**Severity:** important

Done: the `PUT` and `DELETE` endpoints on `/employees/{employeeId}/payrolls/{paymentId}`
were removed from `EmployeesController`. Corrections are via reverse payroll payments.
Note: `IPaymentService.UpdateAsync`/`DeleteAsync` are now unreachable dead code — a small
follow-up cleanup.

## TransactionLine fixed-amount discount

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: discount is percentage only (`UnitPrice × Quantity × (1 - Discount/100)`).
Target: support both percentage and fixed-amount discounts per line and per transaction.

## Category optional on Product

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** nice-to-have

Current: Category required.
Target: optional.

## Address structure

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** nice-to-have

Current: Address is lat/lng only.
Target: text address field; Order.DeliveryAddress optional.

## OpenAPI route casing consistency

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** nice-to-have

Current: inconsistent `{Id}` vs `{id}`.
Target: consistent lowercase.

---

## Added by Phase 1 audit

Audit completed 2026-05-17 by direct codebase read. All gaps predicted from the
initial schema/OpenAPI review (the items above this section) are **confirmed**. The
findings below add module-level detail, exact code locations, and a few items the
initial review did not capture. Format: entity/endpoint → current state → target
state → severity → notes.

### Multi-tenancy — confirmed (RESOLVED 2026-05-17)

This gap was confirmed by the audit and then fixed the same day — see the
"Multi-tenancy enforcement" section above for the implemented solution.

- **All tenant-scoped entities** → audit found `OrganizationId` only on `User`/`Role`.
  → Resolved: renamed to `TenantId`; all tenant-scoped entities now implement
  `ITenantScoped` with a `TenantId` column + index.
- **ApplicationDbContext** → audit found no query filter and no tenant accessor.
  → Resolved: EF Core global query filters + insert-time `TenantId` stamping;
  `ITenantAccessor` resolves the tenant from the `tenant_id` JWT claim.

### Audit log — confirmed

- **No audit infrastructure** → no audit entity, no DbSet, no `SaveChangesInterceptor`
  registered in `ApplicationDbContext` (only `ApplyConfigurationsFromAssembly`). →
  Target per rules.md #12-14. → **Blocker.**

### Transactions

- **TransactionRecord** (`src/Ombor.Domain/Entities/TransactionRecord.cs`) → no
  `OriginalTransactionId`, no `InventoryId`. → Add both; `OriginalTransactionId`
  required for refund types. → **Blocker.**
- **TransactionType enum** (`src/Ombor.Domain/Enums/TransactionType.cs`) → values are
  Sale, Supply, SaleRefund, SupplyRefund only. → Add `WriteOff`. → **Blocker.**
- **Refund logic** → `TransactionService.cs` carries `// TODO: Add logic for refunds`
  (line 161). No `OriginalTransactionId` requirement, no type-match, no refund-of-refund
  block, no refunded-quantity cap. → Implement rules.md #2-6. → **Blocker.**
- **Stock update** → `TransactionService.UpdateProducts` mutates
  `Product.QuantityInStock` directly (lines 178, 192) and never touches
  `InventoryItem`. → Stock-in/out must update `InventoryItem`; `Product.QuantityInStock`
  removed. → **Blocker.** Notes: negative stock *is* blocked (line 187) but against the
  wrong (deprecated) field.
- **Endpoints** → `TransactionsController` exposes GET/POST only — no PUT/DELETE.
  Immutability respected at endpoint level. → No change. → Clean.

### Inventory

- **InventoryItem** (`src/Ombor.Domain/Entities/InventoryItem.cs`) → no `AverageCost`
  field. → Add `AverageCost` (decimal), atomic update on stock-in per rules.md #10. →
  **Blocker.**
- **Transfers** → no `Transfer` entity, no endpoint. `InventoryService` does CRUD on
  the `Inventory` (warehouse) entity only — it never manages `InventoryItem` stock. →
  Add `Transfer` entity + endpoint. → **Blocker.**
- **InventoryItem is effectively unused for live stock** → seeded but never updated by
  transactions; real stock flows through `Product.QuantityInStock`. → Make
  `InventoryItem` the sole source. → **Blocker.** (root of the dual-source bug)

### Archive

- **Product, Partner** → extend `EntityBase` (id only); no `IsDeleted`.
  `AuditableEntity` has `IsDeleted` but is unused by these entities. → Add soft-delete. →
  **Blocker.**
- **DeleteAsync** → `ProductService.DeleteAsync` and `PartnerService.DeleteAsync` call
  `context.X.Remove(entity)` — hard delete. `ProductService` also deletes image files. →
  Switch to soft-delete + default query filter. → **Blocker.**

### Payroll immutability

- **EmployeesController** → exposes `PUT /employees/{employeeId}/payrolls/{paymentId}`
  and `DELETE /employees/{employeeId}/payrolls/{paymentId}` (lines 111, 157), wired to
  `paymentService.UpdateAsync` / `DeleteAsync`. → Remove both; corrections via reverse
  payroll payments. → **Important.** (violates rules.md #1)

### Products & Categories

- **Product.Category** → required (`required virtual Category`, non-nullable
  `CategoryId`). → Make optional. → **Nice-to-have.**
- **Product.QuantityInStock** → still writable; consumed across DTOs, mappings,
  validators, and the test data generators. → Remove; ~15 referencing files to clean. →
  **Important.**
- **Pagination** → `ProductService.GetQuery` filters/searches but never pages
  (no `.Skip()/.Take()`); returns the full table. → **Important.**
- **ProductService** → clean apart from the above. CRUD + images well structured.

### Partners

- **Partner.Balance** → stored `decimal Balance` field still on the entity, unused —
  `PartnerService` reads the `View_PartnerBalance` DB view instead. → Remove the dead
  field. → **Nice-to-have.**
- **Opening balance** → not captured as an auditable event. → Model as a ledger event. →
  **Important.**
- **Pagination** → `PartnerService.GetAsync` filters/searches but never pages. →
  **Important.**
- **Partner balance view** → `View_PartnerBalance` works as designed (advances, payable,
  receivable). → No change. → Clean.

### Templates

- **TemplatesController / TemplateService** → full CRUD; no rule violations (templates
  are mutable by design). → No change. → Clean.

### Payments

- **PaymentsController** → exposes GET/POST only — immutability respected. But
  `GetPaymentByIdAsync` (line 24) throws `NotImplementedException`. → Implement it. →
  **Nice-to-have.**

### Auth & Users

- **AuthService** → registration/tenancy flow is correct: creates a `Tenant` (renamed
  from `Organization` on 2026-05-17), sets `User.TenantId`, issues JWT + refresh token.
  The access token now also carries the `tenant_id` claim. → No structural change. →
  Note. See Pre-production hardening below for the disabled checks.

---

## Pre-production hardening

Not rule divergences — leftover testing shortcuts that must be reverted before any
real deployment.

### Debug delay in ProductsController

**Status:** complete (2026-05-17)
**Severity:** blocker (pre-production)

Done: the `await Task.Delay(4000)` calls were removed from `ProductsController` POST and PUT.

### Disabled password verification

**Status:** complete (2026-05-17)
**Severity:** blocker (pre-production)

Done: `AuthService.LoginAsync` calls `VerifyPassword(user, request.Password)` — login now
validates the password.

### Disabled SMS sending

**Status:** not started — intentionally deferred
**Severity:** important (pre-production)

Current: `AuthService.RegisterAsync` has `smsService.SendMessageAsync(message)` commented
out — registration OTP is never delivered. Left disabled on purpose: re-enabling it now
would break local development/registration if no SMS provider is reachable.
Target: restore the call just before onboarding real users.