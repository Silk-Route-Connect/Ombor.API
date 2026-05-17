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

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: not implemented.
Target: `AverageCost` (decimal) field on InventoryItem; atomic update on every stock-in event.
Formula in rules.md #10.

## WriteOff transaction type

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: TransactionType enum missing WriteOff.
Target: WriteOff value added; transaction has no partner; decrements inventory only.

## Inter-warehouse transfers

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: no transfer entity or endpoint.
Target: Transfer entity with FromInventoryId, ToInventoryId, lines, status, audit-logged; updates both inventories atomically.

## Transaction-to-warehouse linkage

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: TransactionRecord has no InventoryId.
Target: InventoryId on TransactionRecord; required for transaction types that affect stock.

## Refund linking

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: TransactionRecord has no OriginalTransactionId.
Target: OriginalTransactionId on TransactionRecord; required for refund types; validation per rules.md #2-6.

## Archive mechanism

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: DELETE endpoints hard-delete.
Target: IsDeleted flag on Product and Partner; soft-delete; filter from default queries; never hard-delete.

## Audit log

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker

Current: no audit log.
Target: per rules.md #12-14. Single table, EF Core interceptor, narrow scope.

## Opening balance and opening stock as events

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: opening balance/stock as raw number fields.
Target: ledger events with actor and timestamp; balance/stock derived from event chain.

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

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: field exists and is writable; dual source of truth bug.
Target: removed; InventoryItem is sole source.

## Payroll immutability

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important

Current: PUT/DELETE endpoints on `/employees/{id}/payrolls`.
Target: endpoints removed; corrections via reverse payroll payments.

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

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker (pre-production)

Current: `ProductsController` POST and PUT both call `await Task.Delay(4000)` (lines
75, 98).
Target: remove both calls.

### Disabled password verification

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** blocker (pre-production)

Current: `AuthService.LoginAsync` has `VerifyPassword(user, request.Password)` commented
out (line 108) — login currently succeeds with any password.
Target: restore the call.

### Disabled SMS sending

**Status:** not started (confirmed by Phase 1 audit)
**Severity:** important (pre-production)

Current: `AuthService.RegisterAsync` has `smsService.SendMessageAsync(message)` commented
out (line 47) — registration OTP is never delivered.
Target: restore the call before onboarding real users.