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
**Status:** in progress — **M2a + M2b done**. M2a: additive payment schema (`PaymentSourceType`, `PaymentComponent.SourceType`/`WalletId`, `PaymentAllocationType` +`TransactionSettlement`/`AdvanceCredit`, `Payment.WalletId`/`Number`; migration `Add_Payment_Source_And_Wallet_Fields`; legacy fields kept so the build/suite stayed green). M2b: redesigned standalone `POST /api/payments` (wallet source → settlements → excess-as-advance, rule-8 by construction, rule-40 gating, P-### number) + `PaymentRecord`/`PaymentSource`/`PaymentAllocationEntry` DTOs + `GET /payments`,`/{id}`,`/form-data`,`/outstanding`; legacy `PaymentDto` methods kept for the payroll/partner consumers. 9 new payment integration tests green. **M2c started**: rewrote `View_PartnerBalance` onto the source/allocation model (advances from `AdvanceCredit` net of `Advance`-source draws; corrected the balance sign so positive = partner owes us) and fixed `PartnerBalance.Total` to match (migration `Rewrite_PartnerBalance_View_For_Payment_Model`; integration test green). **M2c done (code; integration verification pending Docker)**: redesigned `CreateTransactionRequest` (`walletId`/`paidAmount`/`settlements`/`overpayment` + line `discountType`) and reworked `TransactionService.CreateAsync` to build the source/allocation payment inline (settle this transaction first → other `settlements` → excess as advance/change, rule 8 by construction, rule-40 gate), atomic with stock movement. Rewrote `TransactionRequestFactory`, the multipart serializer, `TransactionTestsBase` helpers, and the Sale/Supply integration tests onto the new model. `dotnet build` + unit suite (308) green; **integration suite not run locally (no Docker) — needs a Podman run to confirm the money/stock invariants and the rewritten tests.** Then **M2d** payroll onto the new model + remove PUT/DELETE; **M2e** wire wallet balance (payment components) + `advancesHeld`; **M2f** drop legacy payment fields + seeder rework + employee/template test fixes + suite green.
**Depends on:** M1 (payments reference wallets).
- Update the payment model to the redesigned shape: sources `{Wallet, Advance}`, allocations `{TransactionSettlement, AdvanceCredit, ChangeReturn}`. Drop `PaymentMethod`/`Currency`/`ExchangeRate`; replace the legacy `PaymentAllocationType` values.
- Enforce the rule-8 identity (sources = settling allocations; ChangeReturn excluded), advance-as-claim (rule 11), advance gating on zero debt (rule 40), change-return-as-memo.
- Re-point Transactions, Refunds, and Payroll at the updated payment fields (relationship unchanged; field shape changes). Payroll drops legacy currency/method/rate.
- `GET /api/payments/form-data`, `GET /api/payments/outstanding?partnerId=` (FIFO oldest-first).

**Decisions & notes:**
- **Folded-in failing tests (user-confirmed):** this milestone also fixes the **22 pre-existing integration failures** — 20 `CreateTransactionTests` (the transaction-create flow reworked here; test factory must supply `InventoryId`), 1 `CreateEmployeeTests` (stale assertion: expects validation key `"FullName"`, request field is `"Name"`), 1 `UpdateTemplateTests` (hardcoded `/api/templates/1`, fragile to shared-DB order). The employee/template two are quick test fixes carried here so the suite goes fully green with M2.
- **User decisions (locked):** existing payment rows are **dropped & recreated** (no legacy→new data conversion); **payroll PUT/DELETE is removed** (payroll becomes immutable, corrections via reverse payment).
- **Additive-first strategy:** M2a/M2b add the new fields/flows while keeping the legacy `Method`/`Currency`/`ExchangeRate` columns and the legacy `PaymentAllocationType` values (Sale/Supply/SaleRefund/SupplyRefund/AdvancePayment), so every commit builds and the suite stays green. The legacy columns/values + the legacy `PaymentService` methods (`CreateAsync(CreatePaymentRequest)` which throws, `CreateAsync(CreateTransactionPaymentRequest)`, `GetAsync`→`PaymentDto`, `GetByIdAsync`, payroll create/update/delete) are removed in **M2f** with a subtractive migration once no consumer reads them.
- `GET /api/payments` + `/{id}` now return the redesigned `PaymentRecordDto` (M2b). `View_PartnerBalance` was rewritten onto the new model (M2c) — that earlier risk is closed.

#### M2c transaction-create rework — design handoff (read before resuming)
The remaining M2c work reworks `TransactionService.CreateAsync` + `CreateTransactionRequest` onto the source/allocation payment model and rewrites the 20 `CreateTransactionTests`. Precise design:

- **New `CreateTransactionRequest`** (multipart; keep `Type` = Sale/Supply/SaleRefund/SupplyRefund): `PartnerId`, `Type`, `int? InventoryId` (required for stock types), `CreateTransactionLine[] Lines`, `int? WalletId`, `decimal PaidAmount`, `SettlementInput[]? Settlements`, `string Overpayment` ("change"|"advance"), `Notes?`, `Attachments?`, `int? OriginalTransactionId`. **`CreateTransactionLine` gains `DiscountType`** (add `Contracts.Enums.DiscountType {Percentage, Fixed}`; map to the domain enum; line total via rule 37 — already on the entities from M0). Drop the legacy `CreatePaymentRequest[] Payments` / `CreateDebtPaymentRequest[] DebtPayments` / `ShouldReturnChange` from this request.
- **Direction** is derived from `Type` (not user-set here): Sale→Income, Supply→Expense, SaleRefund→Expense, SupplyRefund→Income (`EnumExtensions.GetPaymentDirection`).
- **CreateAsync flow** (all inside one `BeginTransactionAsync`): validate → move stock (`UpdateProducts`, unchanged: Supply/SaleRefund stock-in + WAC, Sale/SupplyRefund stock-out hard-blocked) → add transaction, `SaveChanges` (so its Id + row exist for the debt query) → if `WalletId` + `PaidAmount>0`, build the payment → `SaveChanges` → commit. `mapper.ToEntity`/`ToDto` and `UpdateProducts` mostly carry over; rip out the legacy payment bits.
- **Payment build** (the heart): `settleThis = min(PaidAmount, transaction.TotalDue)`; `settlementsTotal = Σ Settlements` (other transactions, each validated `≤ remaining`); `excess = PaidAmount − settleThis − settlementsTotal`. One Payment (Type=Transaction, derived Direction, Partner, Wallet, `P-#` number). Allocations: a `TransactionSettlement` for this transaction (`settleThis`, then `transaction.AddPayment(settleThis)`) + one per `Settlements` entry (+ `AddPayment`). For `excess>0`: **"advance"** → `AdvanceCredit(excess)` **gated by rule 40** (see below) and the wallet **source component = PaidAmount**; **"change"** → `ChangeReturn(excess)` memo and the wallet **source component = PaidAmount − excess** (net of change, rule 15). Rule-8 identity (source = settling allocations, ChangeReturn excluded) then holds by construction in both branches.
- **Rule-40 gating:** after the transaction is saved, `remainingDebt = ComputeSettlableDebt(partner, direction) − settleThis − settlementsTotal` (reuse the helper in `PaymentService`; settlable types are Sale+SupplyRefund for Income, Supply+SaleRefund for Expense). If `Overpayment=="advance"` and `remainingDebt > 0` → 400.
- **Refunds** go through the same endpoint (Type=SaleRefund/SupplyRefund + `OriginalTransactionId`); refund-rule validation (`ValidateRefundOrThrowAsync`) and stock already exist. A refund's payment (cash returned) uses the derived direction; the simple case needs no special handling beyond the flow above.
- **Tests:** rewrite `TransactionRequestFactory` (supply `InventoryId` + `WalletId` + `PaidAmount` + `Overpayment`) and the `CreateTransactionTests` Sale/Supply cases against the new model — set opening balances via a Deposit (advance) or an open transaction, and recompute the `expectedBalance`/`expectedAdvance`/`expectedDebt` against the rewritten `View_PartnerBalance` (positive = partner owes us). The legacy test helpers `CreateAdvancePaymentAsync` (writes an `AdvancePayment` allocation) and the `AccountBalance` "credit" path must be replaced with new-model equivalents (`AdvanceCredit` / `Advance` source).
- **Deferred / known transitional gaps** (close by M2f unless noted): `PaymentRecord.AllocationSummary` returns empty; `CreatedBy`/`Period`/`Salary` return null (payroll = M2d; `CreatedBy` needs a column or audit lookup); standalone **Withdrawal** (drawing an advance via an `Advance` source) isn't built in `CreateRecordAsync` yet; wallet balance does **not** yet include payment components (that's **M2e** — `WalletService` balance is still opening+transfers, `advancesHeld=0`).

**M2c transaction-create — implementation notes (done in code):**
- **`discountType` wire format (user decision):** backend accepts the canonical enum names `Percentage`/`Fixed` (`Contracts.Enums.DiscountType`, case-insensitive); the frontend's `"pct"/"fixed"` is a **frontend bug**, not worked around in the backend. Started a new running doc `docs/frontend-fixes.md` (+ pointer in `doc-map.md`) to collect every frontend-side fix the backend redesign forces — seeded with the discount-type names, transaction multipart vs JSON, and the single-endpoint `type` shape.
- **`overpayment` is the enum `OverpaymentHandling { Change, Advance }`** (not a raw string). The contract's lowercase `"change"/"advance"` bind cleanly because enum parsing is case-insensitive in both form-binding and JSON (the words match, only case differs) — no converter needed.
- **Payment build lives inline in `TransactionService.BuildPaymentAsync`** (not via the legacy `PaymentService.CreateAsync(CreateTransactionPaymentRequest)`, which is now dead and removed in M2f). `ComputeSettlableDebtAsync` + `NextPaymentNumberAsync` were extracted from `PaymentService` into a shared `PaymentCalculationExtensions` (on `IApplicationDbContext`) and are reused by both. `TransactionService` no longer depends on `IPaymentService` (constructor param removed). The dead `PaymentService.ValidateOrThrowAsync(CreateTransactionRequest, …)` (referenced the removed `Payments`/`DebtPayments`) was deleted.
- **Advance-as-source not in transaction-create:** the new request has only a wallet source, matching the target contract's `CreateTransactionEntryRequest` — paying a sale *from* an existing partner advance is the deferred Withdrawal/`Advance`-source feature, not a regression. The legacy `AccountBalance`/partner-advance-draw validation path was removed accordingly.
- **Rule-8 holds by construction:** advance branch → wallet source = `PaidAmount` = settlements + `AdvanceCredit`; change branch → wallet source = `PaidAmount − change` = settlements, with `ChangeReturn` excluded (rule 15). **Rule-40 gate** computes settlable debt (incl. the just-saved transaction at `TotalPaid=0`) minus `settleThis` and `settlementsTotal`, before applying any `AddPayment`.
- **Read DTO:** `TransactionLineDto` gained `DiscountType` (string), projected in both `ToDto` and the `GET` list query.
- **Test scope (user decision):** M2c rewrote only the transaction tests (now focused Fact/Theory cases against the new model, with self-contained product/inventory/stock/wallet seeding and an `AdvanceCredit`-based advance helper). The `CreateEmployeeTests` (`FullName` vs `Name`) and `UpdateTemplateTests` (`/api/templates/1`) fixes stay in **M2f** — after M2c the integration suite still has those 2 known, unrelated failures.
- **Refund reason (added per user — the contract has it, so it can't be skipped):** `TransactionRecord.RefundReason` column (nullable `nvarchar(500)`, migration `Add_Transaction_RefundReason`) + `RefundReason` on `CreateTransactionRequest` and `TransactionDto` (+ `OriginalTransactionId` on the read DTO); mapped in `ToEntity`/`ToDto`/`GET`; validator requires a non-empty reason for SaleRefund/SupplyRefund.

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
- 2026-06-20 — M2c transaction-create rework implemented on branch `redesign/transaction-payments`: new `CreateTransactionRequest` (`walletId`/`paidAmount`/`settlements`/`overpayment` + line `discountType`); `Contracts.Enums.DiscountType` + `OverpaymentHandling`; `TransactionService.CreateAsync`/`BuildPaymentAsync` build the source/allocation payment atomically with stock (rules 8/10/15/40); shared `PaymentCalculationExtensions` (debt + payment number) reused by both services; legacy transaction-payment path no longer called; new `docs/frontend-fixes.md`. Transaction tests + factory + multipart serializer rewritten onto the new model. Build + unit (308) green; integration suite needs a Podman run (no Docker locally). discountType `pct/fixed` and the 2 employee/template test fixes deferred per user. Refund reason added end-to-end (column + migration `Add_Transaction_RefundReason` + request/DTO/validator wiring) per user — the contract has it, so it's not skippable.
