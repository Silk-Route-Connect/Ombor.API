# Ombor Backend — Redesign Plan

**Status:** active working tracker. Claude Code updates this doc at the end of every backend session — marking progress, recording decisions made, and noting anything discovered that changes a later milestone.
**Last updated:** 2026-06-18 (created from audit findings)

Grounded in `docs/audit-findings.md`. The backend is a **migration of a substantial legacy implementation to the redesigned model**, not a greenfield build — multi-tenancy, the audit interceptor, partial WAC, and refund-rule validation already exist; the payment model, Wallets, and several read models do not. Behavior is specified by `business-rules.md`; shapes by `backend-contract.md`; server-side logic by `backend-complexity-notes.md`. When any of those conflict, the rule wins — flag it.

## How to use this doc

- One milestone at a time, in order — later milestones depend on earlier ones.
- A milestone is **done** only when build passes, the full test suite passes, and its invariants have integration coverage (per `CLAUDE.md`).
- At session end: set the milestone's status, fill its **Decisions & notes**, and if something surfaced that changes a later milestone, edit that milestone's entry so the change isn't lost.
- Do not silently re-scope a milestone. If scope should change, note it here and raise it.

Status legend: `not started` / `in progress` / `done` / `blocked`.

---

## Settled decisions (apply throughout — do not relitigate silently)

- **Naming:** entity is `Organization` / `OrganizationId` (rename from `Tenant`). Code conforms to `business-rules.md`.
- **Balances:** persisted **projections** derived from events, recomputed in the same transaction as the event, never hand-mutated, always tenant-scoped. `PartnerBalance` is the projection; `Partner.Balance` is obsolete and removed.
- **Partner ledger:** **derive-on-read** from transactions + payments + opening balance. **No `PartnerLedgerEntry` entity/table.**
- **Transaction create:** stays `POST /api/transactions` with `type` (Sale/Supply/SaleRefund/SupplyRefund) — no separate `/refund` endpoint. Request is **multipart/form-data** (transactions carry attachments). *(Frontend currently sends JSON — tracked as a frontend bug, not a backend change.)*
- **Migration policy:** Code may draft migrations and apply them to the local/dev DB; production apply is human-gated. Never run destructive ops against the production connection string.
- **Discounts:** `discount` + `discountType` (`Percentage`/`Fixed`) persisted on every line type; `Fixed` = currency off the whole line, clamped to line gross; never converted to percentage.
- **Tests:** heavy integration on money/stock invariants, thin focused unit elsewhere; `Ombor.TestDataGenerator` for setup; Docker required for the Testcontainers integration suite.

## Out of band (not milestones — track separately)

- 🔴 **Rotate committed production secrets** (DB creds, JWT key, Eskiz SMS token, Sentry DSN in `appsettings.Production.json` + `docker-compose`) and move to user-secrets/env vars.
- **Error-key casing:** confirm whether the frontend matches `errors` keys case-insensitively; if not, camelCase the `ValidationProblemDetails` dictionary keys (and emit `lines[0].quantity`-style indexed keys). Needs a real frontend sample.

---

## Milestones

### M0 — Schema corrections
**Status:** done — `dotnet build` green; unit suite green (307 pass); integration suite ran via Podman and **all M0 tests pass** (7 new integration tests: PartnerBalance cross-org isolation ×1, Category delete 409 / no-cascade / productCount ×6). ⚠️ **22 pre-existing failures remain in `CreateTransactionTests`** — root cause: `TransactionRequestFactory` builds `CreateTransactionRequest` without `InventoryId`, which the legacy `TransactionService.ValidateOrThrowAsync` requires (→ 400 "InventoryId is required"). Confirmed unrelated to M0: none of the six commits touch that factory, the request DTO, or the validation (the only TransactionService change was a one-line comment). These belong to the legacy transaction-create flow (M2/M4), not schema corrections — left for a separate task.
**Depends on:** nothing — unblocks everything.
Mechanical, each independently shippable (one commit per item, all on `redesign/schema-corrections`):
- ✅ Rename `Tenant`/`TenantId` → `Organization`/`OrganizationId` throughout (entity, claim, query filter, configs, migration).
- ✅ Add `OrganizationId` + scoping interface to `PartnerBalance` (closes the scoping hole).
- ✅ Category→Products `OnDelete(Cascade)` → `Restrict`; 409 reference-gate on category DELETE; `productCount` on `CategoryDto`.
- ✅ Fix `GET /api/payments/{id}` (was `NotImplementedException`).
- ✅ Remove the dead `TransactionType.WriteOff` enum value and its references.
- ✅ Add `DiscountType` to `TransactionLine`, `OrderLine`, `TemplateItem` + recompute logic (rule 37). Persist `discount` + `discountType`.
- **Deferred** `Product.QuantityInStock` removal to M4 (unchanged).

**Decisions & notes:**
- **`ITenantScoped` → `IOrganizationScoped` (renamed, not kept).** Since the whole codebase was already being touched for `TenantId`→`OrganizationId`, renaming the interface was marginal extra churn and keeps the naming consistent. Also renamed: `ITenantAccessor`→`IOrganizationAccessor`, `HttpContextTenantAccessor`→`HttpContextOrganizationAccessor`, `ITenantService`/`TenantService`→`IOrganizationService`/`OrganizationService`, `TenantConfiguration`→`OrganizationConfiguration`, `TenantDto`→`OrganizationDto`, JWT claim `tenant_id`→`organization_id`, seeder `NumberOfTenants`→`NumberOfOrganizations` (+ appsettings keys).
- **`RegisterRequest.TenantName` → `OrganizationName`.** This is a wire-contract field. Renamed because the frontend already sends `organizationName` (per backend-contract §1) and the rule doc names the entity `Organization` — so this fixes a latent mismatch (registration org-name was silently not binding). Flagging since it touches the API surface.
- **The Tenant→Organization migration is non-destructive.** EF scaffolds a drop/create for the renamed table; it was hand-rewritten to `RenameTable` + `sp_rename` for the PK/FKs so no organization rows are lost. Column/index renames EF generated were kept. Verified `has-pending-model-changes` = none, and that the prior constraint names (`PK_Tenant`, `FK_User_Tenant_TenantId`, `FK_Role_Tenant_TenantId`) match the `sp_rename` targets.
- **`PartnerBalance` is a keyless SQL view (`View_PartnerBalance`), not a table.** Org-scoping it meant (a) adding `OrganizationId` to the view SELECT (sourced from `partner.OrganizationId`) via a `CREATE OR ALTER VIEW` migration, and (b) marking the entity `IOrganizationScoped`. The shared filter helper (`ApplyOrganizationQueryFilter`) was changed to **skip the `HasIndex` call for keyless entities** (views can't be indexed) — it now only indexes entities with a primary key.
- **DiscountType defaults differ per table to preserve legacy behavior:** TransactionLine → `Percentage` (its `Total` was a percentage formula); OrderLine & TemplateItem → `Fixed` (they subtracted a currency amount). Set via C# property initializers (so new in-memory entities are never the invalid CLR `0`) **and** `HasDefaultValue` (so the migration backfills existing rows). EF emits a benign "sentinel" warning about the enum's CLR default `0`; harmless here because the initializers guarantee a non-zero value on insert.
- **Discount recompute lives as a single inline expression on each entity's computed total** (not a shared helper) because `TransactionLine.Total` is used inside an EF `Select` and must stay SQL-translatable. Rule-37 clamp is expressed with ternaries (→ `CASE WHEN`).
- **Scope held:** the new `discount`/`discountType` is **persisted** but not yet exposed on the create/update **request DTOs** (`CreateTransactionLine`, order/template line requests) — wiring `discountType` end-to-end through the API contract is left to the per-resource milestones (it's contract-alignment, not schema). M0 only guarantees the column + recompute exist.
- **New shared infra added:** `ConflictException` (Domain) + `ConflictExceptionHandler` (→ 409 `ProblemDetails`), registered ahead of the catch-all handler. Reusable for the M3 partner reference-gate.
- **Migrations added (4):** `Rename_Tenant_To_Organization`, `Scope_PartnerBalance_To_Organization`, `Restrict_Category_Product_Delete`, `Add_Line_DiscountType`. All applied to the model snapshot; none applied to any DB by Code (human-gated). Drafted/verified against the model only — **not run against a database** (no container runtime here).

### M1 — Wallets + org-setup seeding
**Status:** done — `dotnet build` green; unit suite green (308 pass); wallet + scoping integration tests green (12/12 via Podman). Full integration suite: 84 pass / 22 fail — the 22 are all **pre-existing and unrelated to M1** (M1 adds 13 passing tests and zero failures; the failure count was 22 before M1 too). Breakdown of the 22: 20 `CreateTransactionTests` (test factory omits `InventoryId` — legacy create flow, → M2); 1 `CreateEmployeeTests` (stale test asserts validation key `"FullName"`, but the request field is `"Name"`); 1 `UpdateTemplateTests` (hardcoded `/api/templates/1`, fragile to shared-DB execution order). None touch wallets, employees, or templates code changed by M1.
**Depends on:** M0.
- `Wallet` entity + `WalletType` enum (Cash/Card/Bank), CRUD, archive/restore.
- Computed `balance` (opening + wallet-source components + transfers), `advancesHeld`, `ourMoney` (rule 12) — as tenant-scoped projections.
- Opening-balance as an immutable event; inter-wallet transfers (`POST /api/wallets/transfers`) — atomic, immutable, hard-blocked over from-balance, one operation row per side.
- `/wallets/{id}/operations`, `/wallets/{id}/transfers`.
- **Org-setup seeding (rule 42):** one Cash wallet, one warehouse, one category, one ordinary partner at organization provisioning — all ordinary entities, no system flag.

**Decisions & notes:**
- **Balance scope (confirmed with user):** M1 computes `balance = OpeningBalance + Σ(incoming transfers) − Σ(outgoing transfers)`. `advancesHeld = 0` and `ourMoney = balance` until M2 — wallet-sourced payment components and partner advances don't exist until the payment rework. The `WalletService` computation is structured so M2 only needs to add those two terms (a `ToDto` over a `WalletRow` projection of opening + transfer sums).
- **`Wallet` is master data, not an audited money event.** Following the existing convention (Partner/Inventory are not `IAuditable`), `Wallet` is `IOrganizationScoped` only; **`WalletTransfer` is the `IAuditable` immutable money event.** (Rule 26's "audit master-data CRUD" is a separate, not-yet-built concern for *all* master data, so Wallet matches Partner/Inventory.)
- **Transfer atomicity comes for free from the computed model.** A transfer is a single immutable `WalletTransfer` row; both wallets' balances derive from it, so there are no stored balances to update on either side — the move can't half-apply. ⚠️ **Known limitation:** the hard-block (read source balance → insert) is not serialized, so two concurrent transfers from the same wallet could both pass the check and overdraw. Acceptable for low-concurrency MVP; revisit with a row lock / check if it bites.
- **Wallet operations are a derived read model**, not a stored table: the running ledger is opening balance + transfers (in/out), newest-first, `balanceAfter` reconciling to the current balance. There is **no "Opening" operation row** — the contract's `WalletOperationKind` has no such value, so the running balance simply *starts* at the opening balance (complexity notes §H). Payment operations get added in M2.
- **`CreatedAt`/`CreatedBy` are set in the service** (`DateTimeOffset.UtcNow`, `currentUser.UserId?.ToString()`). Nothing else in the codebase stamps `AuditableEntity.CreatedAt/CreatedBy` today — a broader gap left as-is.
- **Org-setup seeding runs in `AuthService` registration** (after Organization + owner User), via a dedicated `IOrganizationSetupService` that pins the new org through the accessor so the 4 starter rows stamp correctly (there's no JWT during registration). Starter names are RU-locale (`Касса`, `Основной склад`, `Основная`, `Розничный покупатель`). It is **not** wrapped in a transaction with the user creation (matching the existing non-transactional registration flow); a seeding failure would leave a usable org the user can populate manually.
- **Migration added (1):** `Add_Wallets` (Wallet + WalletTransfer tables, Restrict FKs, unique `{OrganizationId, Name}`, org indexes from the global filter). Verified against the model and exercised by the integration suite via Podman; not applied to any non-test DB by Code.
- _(correction to the M0 note)_ the 22 pre-existing failures were **never all `CreateTransactionTests`** — it's 20 transaction + 1 employee + 1 template (see M1 status). The non-transaction two are stale/fragile tests unrelated to the redesign.

### M2 — Payment field rework
**Status:** in progress — **M2a + M2b done**. M2a: additive payment schema (`PaymentSourceType`, `PaymentComponent.SourceType`/`WalletId`, `PaymentAllocationType` +`TransactionSettlement`/`AdvanceCredit`, `Payment.WalletId`/`Number`; migration `Add_Payment_Source_And_Wallet_Fields`; legacy fields kept so the build/suite stayed green). M2b: redesigned standalone `POST /api/payments` (wallet source → settlements → excess-as-advance, rule-8 by construction, rule-40 gating, P-### number) + `PaymentRecord`/`PaymentSource`/`PaymentAllocationEntry` DTOs + `GET /payments`,`/{id}`,`/form-data`,`/outstanding`; legacy `PaymentDto` methods kept for the payroll/partner consumers. 9 new payment integration tests green. **M2c started**: rewrote `View_PartnerBalance` onto the source/allocation model (advances from `AdvanceCredit` net of `Advance`-source draws; corrected the balance sign so positive = partner owes us) and fixed `PartnerBalance.Total` to match (migration `Rewrite_PartnerBalance_View_For_Payment_Model`; integration test green). Remaining in **M2c**: redesign `CreateTransactionRequest` (`walletId`/`paidAmount`/`settlements`/`overpayment`/line `discountType`) + rework `TransactionService.CreateAsync` to build the source/allocation payment (settle this transaction + others, overpayment → change/advance, rule 8/40) + rewrite `TransactionRequestFactory` and the 20 transaction tests. This is the heaviest, most safety-critical change (atomic stock+money) and forces the request/factory/test rewrite in one coupled change — kept for a focused pass. Then **M2d** payroll onto the new model + remove PUT/DELETE; **M2e** wire wallet balance (payment components) + `advancesHeld`; **M2f** drop legacy payment fields + seeder rework + employee/template test fixes + suite green.
Known transitional gaps to close by M2f: `PaymentRecord.AllocationSummary` returns empty and `CreatedBy`/`Period`/`Salary` return null (payroll lands in M2d; `CreatedBy` needs a column or audit lookup).
**Depends on:** M1 (payments reference wallets).
- Update the payment model to the redesigned shape: sources `{Wallet, Advance}`, allocations `{TransactionSettlement, AdvanceCredit, ChangeReturn}`. Drop `PaymentMethod`/`Currency`/`ExchangeRate`; replace the legacy `PaymentAllocationType` values.
- Enforce the rule-8 identity (sources = settling allocations; ChangeReturn excluded), advance-as-claim (rule 11), advance gating on zero debt (rule 40), change-return-as-memo.
- Re-point Transactions, Refunds, and Payroll at the updated payment fields (relationship unchanged; field shape changes). Payroll drops legacy currency/method/rate.
- `GET /api/payments/form-data`, `GET /api/payments/outstanding?partnerId=` (FIFO oldest-first).

**Decisions & notes:**
- **Folded-in failing tests (user-confirmed):** this milestone also fixes the **22 pre-existing integration failures** — 20 `CreateTransactionTests` (the transaction-create flow reworked here; test factory must supply `InventoryId`), 1 `CreateEmployeeTests` (stale assertion: expects validation key `"FullName"`, request field is `"Name"`), 1 `UpdateTemplateTests` (hardcoded `/api/templates/1`, fragile to shared-DB order). The employee/template two are quick test fixes carried here so the suite goes fully green with M2.
- _(from M0)_ `GET /api/payments/{id}` now returns the **legacy** PaymentDto shape (components carry `Method`/`Currency`/`ExchangeRate`). Reshape it here alongside the rest of the payment rework.
- _(from M0)_ ⚠️ **The `View_PartnerBalance` SQL depends on legacy payment fields** — it filters on `PaymentComponent.Method = 'AccountBalance'`, `PaymentAllocation.Type = 'AdvancePayment'`, and multiplies by `ExchangeRate`. When this milestone drops/renames those fields, **the view migration must be rewritten** (see `Add_Partner_Balance_View` + `Scope_PartnerBalance_To_Organization`) or partner balances will break. Coordinate M2 ↔ M3.

### M3 — Partner balance projection + ledger
**Status:** not started
**Depends on:** M2.
- Rebuild `PartnerBalance` as the tenant-scoped projection derived from events, recomputed in-transaction (it's already `init`-only and not hand-mutated — extend, don't redesign the mutation model).
- `GET /api/partners/{id}/ledger` — derive-on-read from transactions + payments + opening balance; newest-first; running balance reconciles exactly to the stored balance.
- Add `openingBalance` (immutable event at creation, not editable), `isDeletable`, `activityCount`; 409 delete-gate when referenced.

**Decisions & notes:**
- _(from M0)_ `PartnerBalance` is **currently a keyless SQL view** (`View_PartnerBalance`), not a stored projection, and now carries `OrganizationId` + is `IOrganizationScoped`. "Rebuild as the tenant-scoped projection" = decide whether to keep the view or materialize it; either way it stays org-filtered. The view body lives in raw-SQL migrations.
- _(from M0)_ The 409 reference-gate can reuse the `ConflictException` + `ConflictExceptionHandler` added in M0 (Category delete).

### M4 — Inventory / WAC consolidation + Stock Adjustments + Transfers + Warehouses
**Status:** not started
**Depends on:** M0–M3 for the money side; independent on the stock side.
- Consolidate WAC across all five stock-in/out paths (Supply, opening stock, transfer receipt, sale refund, stock-adjustment increase) — currently partial and split across services; verify consistency with integration tests.
- Remove `Product.QuantityInStock`; `InventoryItem` sole source (rule 17). Add `totalStock` + value-weighted `averageCost` aggregates + `/products/{id}/movements`.
- **Stock Adjustments** (absent): entity + `StockAdjustmentDirection` enum, immutable, mandatory reason, Decrease records cost at WAC as a loss line, negative-stock hard-blocked.
- **Transfers** to target shape (`fromWarehouseId`, `createdBy`, drop unused status).
- Rename `/api/inventories` → `/api/warehouses`; add `/stock`, `/movements`, `/archive`, `/restore`, computed totals.

**Decisions & notes:**

### M5 — Orders
**Status:** not started
**Depends on:** M4 (stock check at deliver) + M2 (Sale promotion settles payment).
- Warehouse chosen at **delivery confirmation** (`DeliverOrderRequest` gains `warehouseId`); hard stock check at that warehouse; **promote to a real Sale** (currently deliver doesn't promote — sets nothing).
- Status history (`OrderStatusEvent`), `saleId` link, `deliveryDate`/`deliveryTime` split, per-line `sku`/`measurement`.
- `PUT` warehouse semantics: undefined=keep, null=clear, value=set.

**Decisions & notes:**
- _(from M0)_ `OrderLine` now has `DiscountType` (defaulting to `Fixed`, preserving the old fixed-amount subtraction) and `TotalPrice` applies rule 37 with a gross clamp. Wire `discountType` through the order create/update request DTOs here (M0 only added persistence + recompute, not the request-shape).

### M6 — Debts + Dashboard read models
**Status:** not started
**Depends on:** everything above (both project transactions/payments).
- `GET /api/debts` — derived projection over unpaid/partially-paid transactions; direction, remaining, ageDays, overdueDays. Not a stored entity.
- `GET /api/dashboard?period=` — KPIs, series, aging, top-debtors, recent. Debt figures reconcile with `/debts`. Preserve the deliberate «Просрочено» = 31+ days definition divergence (complexity notes §K).

**Decisions & notes:**

### M7 — Settings / Users
**Status:** not started
**Depends on:** independent of the money path — parked last.
- Org profile (name/address/phone/email/logo) — `GET/PUT /api/settings/organization`.
- User management: invite, deactivate/reactivate (rule 41 — never hard-delete), per-user language preference.

**Decisions & notes:**

---

## Change log

_(Append a dated line whenever a milestone completes or a plan decision changes.)_
- 2026-06-18 — Plan created from audit findings.
- 2026-06-20 — M0 implemented (6 items, one commit each on `redesign/schema-corrections`): Tenant→Organization rename (+`ITenantScoped`→`IOrganizationScoped`, JWT claim, non-destructive rename migration); PartnerBalance view org-scoped; Category delete Restrict + 409 gate + `productCount`; `GET /api/payments/{id}` implemented; `TransactionType.WriteOff` removed; `DiscountType` added to the three line types (rule 37). Build + unit suite green (307). Integration suite written but **not run — no Docker in the dev environment**; needs a user run to finalize DoD.
- 2026-06-20 — M0 integration suite later run via Podman: all M0 tests green; 22 pre-existing failures confirmed unrelated (transaction/employee/template).
- 2026-06-20 — M1 implemented on branch `redesign/wallets` (6 commits): Wallet + WalletTransfer schema; wallets resource (CRUD, archive/restore, transfers, derived operations/transfers read models) with computed balance = opening + transfers; org-setup seeding of the 4 starter rows at registration (rule 42). Build green; unit 308; wallet + scoping integration 12/12 green via Podman; full integration 84 pass / 22 pre-existing fail (zero new). Wallet payment-component + advance terms deferred to M2 (user-confirmed). The 22 legacy failures slotted to M2 (transaction create) per user.
