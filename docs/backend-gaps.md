# Ombor backend — verified gap list (regenerated)

**Regenerated:** 2026-07-09 · **Method:** read-only recon, one auditor per row opening the real code, every open Blocker/Important gap re-checked by an adversarial skeptic. **Canon:** `docs/business-rules.md` (rule wins on any conflict).
**Last updated:** 2026-07-14 — harvested the out-of-band open items (secrets rotation, error-key casing) and the retired `Ombor.Web/docs/backend-deltas.md` queue into the two sections before Part C; A-rows unchanged from the 2026-07-09 recon.

Statuses: **Done** / **Partial** / **Missing** / **Superseded**. Severity (open gaps only): **Blocker** (violates a numbered Hard Rule) / **Important** / **Nice-to-have**. Every verdict is backed by `file:line — member` evidence in the per-item detail; "(challenge: …)" records the adversarial re-check.

---

## Summary table

| # | Area | Canon | Status | Severity | One-line |
|---|------|-------|--------|----------|----------|
| A1 | Tenancy | R34 | **Done** | — | Single reflection-applied global query filter over `IOrganizationScoped`; stamping + JWT resolution present. (Fails *open* at org 0 — unreachable today.) |
| A2 | Immutability | R1 | **Done** | — | No PUT/DELETE on any immutable event, at endpoint *and* service level. |
| A3 | Refunds | R2–R7 | **Done** | — | All six checks enforced server-side. One correctness hole: intra-request duplicate-product lines bypass the cumulative cap. |
| A4 | Payment model | R8–R10 | **Partial** | Important | Enums exact; rule-8 balancing is *by construction only* (no reject guard); Payroll/General persist source-only payments. |
| A5 | Balances | R10–12,15 | **Partial** | Important | Balances correctly computed-not-stored; but `ourMoney` = `balance − advancesHeld` with **advancesHeld hardcoded 0** → overstates once any advance exists. |
| A6 | Wallets | R15–16 | **Partial** | Important | Net-of-change ✓, inter-wallet transfer ✓; opening wallet balance is a **raw field, not an audited event** (Wallet not `IAuditable`). (Auditor said Blocker → challenge downgraded.) |
| A7 | Inventory | R17,20–22 | **Partial** | Important | Removal ✓, negative-stock block ✓, create-at-zero ✓; **package-count entry/conversion not implemented**; 400 lacks available-vs-requested. |
| A8 | WAC | R18–19 | **Done** | — | WAC stored, atomically updated on all 5 stock-ins, leaves at WAC. Sale COGS not snapshotted (WAC drifts — see decision). |
| A9 | StockAdjustment | R23–25 | **Done** | — | Direction + mandatory reason, immutable, partner/payment-less, WAC snapshot; no `WriteOff` in enum. |
| A10 | Transfers | domain | **Done** | Nice-to-have | Atomic dual-warehouse, immutable, no money. `TransferLine.Quantity` persisted at (18,2) not (18,3); no status field. |
| A11 | Txn↔warehouse | domain | **Done** | — | `WarehouseId` required (unconditionally) and enforced pre-write. |
| A12 | Archive | R29–32 | **Done** | — | `IsArchived` on exactly the 4 entities; warehouse delete-guard + `isDeletable` share one predicate. |
| A13 | Audit | R26–28 | **Partial** | Important | Money/stock capture complete; **master-data CRUD not audited** (conflicts R26); no Activity-Log read endpoint (R28, v2-deferred). |
| A14 | Opening events | R16,22,partner | **Partial** | **Blocker** | Opening **stock** is an audited event ✓; opening **wallet** and **partner** balances are raw fields → no AuditEntry; partner has **no actor** at all. |
| A15 | Discounts | R37–38 | **Done** | — | Line-level %/fixed both persisted, no txn-level field, total computed. Missing `Discount ≥ 0` guard → negative discount = surcharge. |
| A16 | Currency | R33 | **Done** | — | No currency/FX/USD remnant in live schema/DTOs/enums; legacy columns dropped. |
| A17 | Search | brief | **Missing** | Important | Plain `LIKE` everywhere; **no Cyrillic↔Latin parity** (a Latin query won't match a Cyrillic name). |
| A18 | List endpoints | contract | **Partial** | Important | **Pagination 0/14** (PagedRequest/Response defined, never wired); filter 8/14, search 9/14 with **2 dead search params** (Transactions, Payments). |
| A19 | Category/Address/Routes | R42+model | **Done** | — | Category required + ordinary seeded starter (old "make optional" **superseded**); routes consistent; orphan `AddressDto` to clean up. |
| A20 | Warehouse scale | domain | **Done** | — | No hardcoded warehouse-count ceiling; ~5 warehouses safe. |

**Counts:** Done 12 · Partial 7 · Missing 1 · (0 wholly Superseded — supersessions are sub-targets, see below). **Open gaps:** 1 Blocker · 7 Important · 1 Nice-to-have.

---

## The consequential cluster — master-data & opening-balance auditing (A6 + A13 + A14)

These three rows share **one root cause** and together are the most important finding, because they cut against the product's dispute-grade-audit differentiator:

**`Wallet` and `Partner` (and `Product`, `Category`, `Employee`, `Template`, `Order`, `Warehouse`) do not implement `IAuditable`.** The sole audit producer, `AuditSaveChangesInterceptor.Capture()` (`AuditSaveChangesInterceptor.cs:74`), enumerates only `ChangeTracker.Entries<IAuditable>()`. Consequence chain:

- **No AuditEntry for master-data CRUD** — partner name/phone edits (which `product-brief.md:110` says *must* be logged), product price changes, wallet edits: all invisible to the ledger. Conflicts with **R26** and complexity-note A6 ("capture should be built in from the start"). `IAuditable.cs:3-8` documents the exclusion as deliberate, but **no decision doc records R26's master-data half as superseded**.
- **Opening wallet balance** is a raw `Wallet.OpeningBalance` assignment (`WalletService.cs:55`) — exactly the "raw number assignment" **R16** forbids. Actor+timestamp exist on the row (`CreatedById`/`CreatedAt`), but not as a ledger event.
- **Opening partner balance** is worse — raw `Partner.OpeningBalance` with `OpeningDate` (date-only) and **no `CreatedById` anywhere** on `Partner`, so the figure cannot be attributed to a user. Violates R26's "actor" requirement.
- Opening **stock** is the correct contrast: `OpeningStock` is its own `IAuditable` event → this is the model to mirror.
- **No Activity-Log read endpoint** exists (`grep [Aa]udit/[Aa]ctivity` over Controllers = 0). R28 unsatisfiable until one is added; documented as v2 (`mvp-plan.md:145`).

A14 is the one confirmed **Blocker** (violates numbered rules 16 & 26; the challenge agreed). A6 (wallet slice) and A13 (master-data + read side) are **Important**. **This cluster needs a single decision** (below), not three separate fixes.

---

## Per-item detail

### A1 · Tenancy · R34 — Done
Shared mechanism = a **single EF global query filter applied by reflection** to every `IOrganizationScoped` type, so a new endpoint cannot skip it.
- `IOrganizationScoped.cs:8` — marker interface (`int OrganizationId`).
- `ApplicationDbContext.cs:57-66` — `OnModelCreating` reflects over all entity types, applies the filter to every `IOrganizationScoped`.
- `ApplicationDbContext.cs:85-90` — the **only** `HasQueryFilter`: `e => CurrentOrganizationId == 0 || e.OrganizationId == CurrentOrganizationId`.
- `ApplicationDbContext.cs:71-116` — `StampOrganization` in both `SaveChanges` overrides.
- `HttpContextOrganizationAccessor.cs` + `JwtTokenService.cs:23` — org from `organization_id` JWT claim; `DependencyInjection.cs:36-40` — global `[Authorize]` fallback.
- All 19 canon entities carry `OrganizationId` (coverage is actually broader). No `IgnoreQueryFilters`, no raw SQL anywhere.
- **Decision (hardening, not a live bug):** filter **fails open** at `CurrentOrganizationId == 0` (returns all orgs' rows). Unreachable today. Should it fail *closed* for an authenticated request that resolves org 0?

### A2 · Immutability · R1 — Done
`TransactionsController`, `PaymentsController`, `StockAdjustmentsController`, `TransfersController` expose only GET+POST; service interfaces declare only `Get*`/`Create*`. `PaymentComponent`/`PaymentAllocation` have no controller/service surface. Payroll is create-only via `POST api/employees/{id}/payrolls`. (`EmployeesController` PUT/DELETE act on the `Employee` entity, not payroll — out of R1 scope.)

### A3 · Refunds · R2–R7 — Done
All six checks in `ValidateRefundOrThrowAsync` (`TransactionService.cs:416-481`): R2 both halves, R3 type-match, R4 no-refund-of-refund, R5 cumulative per-product cap across all prior refunds, R6 by construction, R7 mandatory reason (`CreateTransactionValidator.cs:42-47`).
- **Found during recon (correctness):** R5 checks each request line against the *same baseline* without summing sibling lines of the same product **within one request** (`TransactionService.cs:466-481`). A single refund with two lines of the same product (orig qty 10, lines 6+6) each pass `0+6≤10` yet total 12 → over-refund + over-restock. Cross-request enforcement is correct; only intra-request aggregation is missing. **Fixable** (log only).

### A4 · Payment model · R8–R10 — Partial / Important *(challenge: agrees)*
Enums exact (`PaymentSourceType {Wallet, Advance}`, `PaymentAllocationType {TransactionSettlement, AdvanceCredit, ChangeReturn}`); legacy method/currency/rate columns dropped (`migration …182722`). **Rule-8 balancing is guaranteed only by construction** — the request DTO can't express two sides, and both write paths derive allocations from one `Amount`. Holes:
- `PaymentService.CreateAsync` (payroll, `:292-328`) persists a Wallet source with **zero allocations**.
- Partner-less General with excess (`CreateRecordAsync:121` gates AdvanceCredit on `PartnerId is not null`) persists a Wallet source with **no settling allocation**.
- No defensive guard anywhere (validator, entity, interceptor, DB) — `grep CheckConstraint` = 0; `BuildPaymentAsync:316-318` comment literally says "satisfying rule 8 by construction."
- **Decision:** (a) accept by-construction, or add a `sum(sources)==sum(settling)` assert before SaveChanges? (b) are Payroll/General **exempt** from the two-sided identity, or must every payment carry a balancing allocation?

### A5 · Balances · R10–12,15 — Partial / Important *(challenge: agrees, strengthened)*
Core invariants **Done, computed-not-stored:** partner balance is a keyless live view (`PartnerBalanceConfiguration.cs:12` `ToView("View_PartnerBalance")`+`HasNoKey`); wallet balance is a per-read LINQ aggregate filtered to `SourceType==Wallet` (`WalletService.cs:333-353`); ChangeReturn excluded from both; advance-claim consumption modeled in the view. Gaps:
- **`ourMoney` overstates real money.** `WalletService.ToDto:359` hardcodes `const advancesHeld = 0m`; `OurMoney = balance − advancesHeld` → always equals balance. Challenge proved this is reachable: `CreateRecordAsync:98-129` books the **full** amount as a Wallet source *and* an AdvanceCredit for the excess, so a partner's advance physically raises the wallet balance yet `advancesHeld` ignores it. `WalletDto.OurMoney` **is shipped to clients**.
- No write path creates a `PaymentSourceType.Advance` component → advance-draw/Withdrawal claim-consumption read logic is unexercised (documented deferral — recorded in the retired M0–M7 redesign plan; git history).
- **Decision:** per-wallet advance-attribution model (which wallet holds a parked advance; re-attribution on transfer/draw) before `ourMoney` can be right.
- **Fixable (log):** `PartnerService.cs:238` re-derives the balance identity inline instead of reusing `PartnerBalance.Total` — drift risk.

### A6 · Wallets · R15–16 — Partial / Important *(auditor: Blocker → challenge: Important)*
Net-of-change ✓ (`TransactionService.cs:319-329`), inter-wallet transfer ✓ (`WalletTransfer` is `IAuditable`, overdraw-blocked, atomic, immutable). **Opening balance fails as an event** — see the cluster above. Challenge downgraded to Important because `OpeningBalance` is immutable (update touches only `Name`), the balance is fully computed (no drift, hard rule 2 holds), and `CreatedById`/`CreatedAt` are on the row. **Fixable candidate** but see the modeling decision.

### A7 · Inventory · R17,20–22 — Partial / Important *(challenge: agrees)*
- R17 ✓ (`Product.QuantityInStock` gone, column dropped; remaining hits are stale docs).
- R20 ✓ and **centralized**: all 5 stock-out paths (Sale, SupplyRefund, transfer-send, adjustment-decrease, order delivery) route through one `MoveStockAsync` StockOut guard (`StockMovementExtensions.cs:69-79`) → 400 ValidationProblemDetails. **Gap:** the 400 message is generic, omits available-vs-requested (`:74`).
- R22 ✓ (create is definition-only; stock enters via `OpeningStock` events).
- **R21 package handling NOT implemented** (main gap): line entities/DTOs carry only a single decimal `Quantity`; `ProductPackaging.Size` is descriptive metadata no write path consumes → no package-count×size conversion, no retained package count for audit.
- **Superseded:** R21 "integer quantity throughout MVP" → **WS4 decimal(18,3)** domain-wide (`PropertyBuilderExtensions.cs:15` `HasQuantityPrecision`).
- **Decision:** is package-count capture a deferred V2 (like fractional stock) or an in-scope MVP gap?

### A8 · WAC · R18–19 — Done
- AverageCost stored on `WarehouseItem:15` (real column, not a view). WAC recompute formula exact at `StockMovementExtensions.cs:96-104`; all 5 stock-ins update it atomically inside a transaction. SaleRefund + adjustment-increase use `StockInAtCarryingCost` (= current WAC, matches R24).
- Stock leaves at WAC (`:69-79` decrements qty only). Adjustment-decrease snapshots loss = qty×WAC into `StockAdjustment.UnitCost` (`StockAdjustmentService.cs:112`).
- **Deferred (documented):** the P&L that reports decrease-loss distinct from COGS is v2 (`mvp-plan.md:123`); `StockAdjustmentDto` doesn't expose the stored loss cost (fixable).
- **Decision (dispute-grade tension):** Sale stock-out records **no COGS snapshot**; since WAC drifts with later stock-ins, a past sale's exact COGS is **not reconstructable from the ledger** — in tension with hard rule 2. Snapshot COGS per sale line, or accept v2 Reports deferral?

### A9 · StockAdjustment · R23–25 — Done
`StockAdjustment` entity with `Direction` (Decrease/Increase) + mandatory validated `Reason`; immutable (Get/Create only); no `PartnerId`/payment link; Decrease snapshots WAC. `TransactionType` (domain + contract) has **no `WriteOff`**.
- **Superseded:** WriteOff transaction type → StockAdjustment (R23). **Fixable (log):** the retired 2026-06-18 audit claimed `WriteOff=5` exists — stale; that doc was retired 2026-07-14, so no correction is needed.

### A10 · Transfers · domain — Done / Nice-to-have
Real names `FromWarehouseId`/`ToWarehouseId` + `TransferLine`; immutable; both warehouses moved in one `SaveChangesAsync` (`TransferService.cs:103`); negative-stock guard on source fires pre-write; no money/partner.
- **Found during recon:** `TransferLineConfiguration.cs:25` uses `HasCurrencyPrecision()` (18,2) not `HasQuantityPrecision()` (18,3) → a 1.125-unit transfer persists as 1.13 in the audit row though stock moved 1.125. **`TransactionLine.Quantity` has the identical (18,2) issue** — a pattern. Fix needs config + migration. **Fixable (log).**
- **Decision:** canon lists a Transfer "status"; the entity has none (single atomic movement). Intentional?

### A11 · Transaction↔warehouse · domain — Done
`TransactionRecord.WarehouseId` (non-nullable int, required FK, migrated NOT NULL). Enforced in `ValidateOrThrowAsync:339-347` for **all** types (stronger than canon's "stock-affecting only"). Minor: the required check lives in the service, not the FluentValidation validator (still a 400).

### A12 · Archive · R29–32 — Done
`IsArchived` on exactly Product/Partner/Wallet/Warehouse. Archive never reference-gated. Archived wallets/warehouses still counted (explicit "included per rule 31" comments; no `IsArchived` query filter anywhere). **R32 single predicate:** `WarehouseService.IsReferenced:25-31` covers all 6 reference kinds and backs *both* the delete guard and the served `isDeletable` — cannot drift.
- **Fixable (log):** `ProductService.cs:113` throws 400 for its delete gate while Partner/Category throw 409 (consistency nit; Product isn't in hard-rule-6's 409 set).

### A13 · Audit · R26–28 — Partial / Important *(challenge: agrees)*
See cluster. Mechanism solid (`AuditSaveChangesInterceptor` writes immutable AuditEntry with actor/timestamp/before-after/insert-id backfill); all 10 money/stock entities covered; single indexed AuditEntry table (R28 storage exists). **Gaps:** master-data CRUD unaudited (R26); no Activity-Log read endpoint (R28, v2). Also note the legacy `AuditableEntity` who-stamp fields (`CreatedBy`/`UpdatedBy`) are **not populated by any interceptor** — dead partial scaffolding.

### A14 · Opening events · R16,22,partner — Partial / **Blocker** *(challenge: agrees, Blocker)*
See cluster. Opening stock ✓ (audited event). Opening wallet balance = raw field, no AuditEntry. Opening partner balance = raw field, **no actor**. Violates numbered rules 16 & 26 → Blocker.

### A15 · Discounts · R37–38 — Done
`TransactionLine.Discount` + `DiscountType {Percentage, Fixed}` both persisted (`TransactionLineConfiguration.cs:36-51`); `Total` computed (fixed off whole line clamped to gross; % clamped at 100) and `Ignore`d in EF. No txn-level discount field on request or `TransactionRecord`; `TotalDue = lines.Sum(Total)`. Fixed never converted to %. Unit-tested (`LineDiscountTests.cs`).
- **Found during recon:** no `Discount ≥ 0` validation — `Total` only clamps *positive* discounts, so a **negative discount acts as a surcharge** inflating TotalDue (e.g. `%-10` → gross×1.1). **Fixable (log).**

### A16 · Currency · R33 — Done
No currency/FX field on any Domain entity, Contracts DTO, Application service, or current EF snapshot. Legacy `Currency`+`ExchangeRate` dropped (`migration …182722`); `View_PartnerBalance` rewritten to sum raw `Amount`. Remaining hits are frozen historical migrations + naming false positives (`HasCurrencyPrecision`, `UZS_STEP`).

### A17 · Search · brief — Missing / Important *(challenge: agrees)*
Every list/search path (Product/Partner/Category/Order/Employee/Warehouse/Wallet/Template) uses plain EF `.Contains()` → SQL `LIKE '%term%'`. **No transliteration, no normalized column, no custom collation** (`grep Collation|translit|Normalize` = only UI-language settings). A Latin query won't match a Cyrillic name and vice versa — a core Uzbek bilingual-UX gap.
- **Decision:** mechanism — (a) shadow normalized column, (b) runtime term expansion into both scripts, or (c) SQL-side collation/normalization.

### A18 · List endpoints · contract — Partial / Important *(challenge: agrees)*
**Pagination: 0/14.** `PagedRequest`/`PagedResponse` are defined but referenced by **no service/controller/DTO**; every list returns an unbounded `ToArrayAsync`. Filter 8/14, search 9/14.

| Endpoint | Pag | Filter | Search | Evidence |
|---|---|---|---|---|
| Products | N | Y (category, price range, type, archived) | Y | `ProductService.cs:151-196` |
| Partners | N | Y (archived) | Y | `PartnerService.cs:19-28` |
| Categories | N | N | Y | `CategoryService.cs:19-22` |
| Transactions | N | Y (partner, status, type) | **DEAD** (`SearchTerm` never applied) | `TransactionService.cs:488-523` |
| Payments | N | Y (partner, employee, dates, type, dir) | **DEAD** (`SearchTerm` + `TransactionId` never applied) | `PaymentService.cs:210-254` |
| Wallets | N | N | Y | `WalletService.cs:24-26` |
| Warehouses | N | N | Y | `WarehouseService.cs:39-42` |
| StockAdjustments | N | Y (warehouse, product) | N | `StockAdjustmentService.cs:34-39` |
| Transfers | N | Y (warehouse) | N | `TransferService.cs:27` |
| Templates | N | Y (type) | Y | `TemplateService.cs:84-92` |
| Orders | N | Y (status, customer, dates) | Y | `OrderService.cs:219-246` |
| Employees | N | N | Y | `EmployeeService.cs:75-80` |
| Debts | N | N | N | `DebtService.cs:10` (no request DTO) |
| Movements | N | path-id only | N | `MovementService.cs:14,69` |

- **Found during recon:** two **dead query params** — `GET /api/transactions?searchTerm=` and `GET /api/payments?searchTerm=` (and `?transactionId=`) are on the contract but silently ignored → callers get unfiltered results. **Fixable (log):** wire or drop.
- **Decision:** is server-side pagination in the current MVP slice? Wiring it touches every DTO + service return type + controller.

### A19 · Category / Address / Routes · R42+model — Done
Category **required** (validator `GreaterThan(0)`+existence; non-null `CategoryId`; EF `IsRequired`). Org setup seeds **one ordinary** starter category, no system flag (`OrganizationSetupService.cs:16-37`; `Category` has no `IsDefault`/`IsSystem`). Delete reference-gated → 409 (`CategoryService.cs:68-84` + Restrict migration). Routes consistent across all 17 controllers (`api/<resource>`, kebab-case). Address value object = `{ Text?, Latitude?, Longitude? }`, used by `Order.DeliveryAddress`.
- **Superseded:** "make category optional / auto-create protected Default Category" → killed by R42 + product model.
- **Fixable (log):** `AddressDto` is orphaned (used nowhere) and diverges from domain `Address` (omits `Text`, non-nullable coords) — delete or realign.

### A20 · Warehouse scale · domain — Done
No count ceiling on create (`WarehouseService.CreateAsync`/validator); all per-warehouse reads iterate dynamically; dashboard doesn't touch warehouses; DTOs are unbounded arrays / ID-keyed rows. ~5 warehouses safe.

---

## Superseded (dead targets, with the rule that killed each)

| Old target | Killed by | Where verified |
|---|---|---|
| "Make Category optional / auto-create a protected **Default Category**" | **R42** + product model: category required, ordinary seeded starter, no system flag | A19 |
| "**Integer** stock quantity throughout MVP" (R21 as written) | **WS4 decision** → `decimal(18,3)` domain-wide (`HasQuantityPrecision`) | A7, A8, A9 |
| **`TransactionType.WriteOff`** as a stock-loss path | **R23** → `StockAdjustment` replaces it; enum has no WriteOff | A9 |

**Deferred (documented, not gaps):** StockAdjustment-decrease loss reported *distinct from COGS* on the read side → v2 Reports module (`mvp-plan.md:123`; data captured on the immutable event). Activity-Log **screen** → v2 (`mvp-plan.md:145`). Advance-source draw / standalone Withdrawal → deferred (retired redesign plan; git history).

---

## Found during recon (not on the original matrix)

| # | Item | Severity | Evidence |
|---|------|----------|----------|
| F1 | **Intra-request duplicate-product over-refund** — two refund lines of the same product in one request each pass the cap; sum exceeds original qty → over-refund + over-restock | Important (correctness) | `TransactionService.cs:466-481` |
| F2 | **Negative discount = surcharge** — no `Discount ≥ 0` guard; `Total` clamps only positive discounts, so `%-10` inflates TotalDue | Important (correctness) | `CreateTransactionValidator.cs`, `TransactionLine.cs:27-30` |
| F3 | **Dead query params** — `Transactions.SearchTerm`, `Payments.SearchTerm`/`TransactionId` on the contract but never applied → silently unfiltered | Important | `TransactionService.cs:488-523`, `PaymentService.cs:210-254` |
| F4 | **Fractional quantity truncated in audit rows** — `TransferLine.Quantity` & `TransactionLine.Quantity` persisted at (18,2) vs domain (18,3); audit row disagrees with actual stock moved | Nice-to-have | `TransferLineConfiguration.cs:25`, `TransactionLineConfiguration.cs` |
| F5 | **Sale COGS not reconstructable** — no per-line COGS snapshot; WAC drifts → past-sale COGS not derivable from ledger (tension w/ hard rule 2) | Decision | `StockMovementExtensions.cs:69-79` |
| F6 | **Tenancy filter fails open at org 0** — hardening, unreachable today | Nice-to-have | `ApplicationDbContext.cs:90` |
| F7 | **Balance identity duplicated** — `PartnerService.cs:238` re-derives instead of reusing `PartnerBalance.Total` (drift risk) | Nice-to-have | `PartnerService.cs:238` |
| F8 | **Dead audit scaffolding** — legacy `AuditableEntity.CreatedBy/UpdatedBy` who-stamps populated by no interceptor | Nice-to-have | `AuditableEntity.cs:3-10` |
| F9 | **Orphan `AddressDto`** — used nowhere, diverges from domain `Address` | Nice-to-have | `AddressDto.cs:3` |
| F10 | **Product delete uses 400 not 409** — inconsistent with Partner/Category | Nice-to-have | `ProductService.cs:113` |
| F11 | **StockAdjustmentDto omits loss cost** — stored `UnitCost` not surfaced (may be intentional v2) | Nice-to-have | `StockAdjustmentDto.cs:19-34` |

---

## Decisions needed (a human call, not a fix)

1. **Master-data & opening-balance audit scope (A6/A13/A14, the Blocker cluster).** Accept the money/stock-only narrowing as a *recorded supersession* of R26's master-data half, **or** implement master-data + opening-balance auditing (make `Partner`/`Wallet`/… `IAuditable`; add a `CreatedById` to `Partner`; model opening wallet/partner balance as events mirroring `OpeningStock`). Plus: minimal Activity-Log read endpoint now, or hold to v2?
2. **Rule-8 enforcement & Payroll/General shape (A4).** By-construction acceptable, or add a defensive balance assert? Are Payroll/General exempt from the two-sided identity?
3. **`ourMoney` / per-wallet advance attribution (A5).** How does a parked advance attribute to a wallet, and re-attribute on transfer/draw?
4. **Package-count capture (A7).** In-scope MVP or deferred like fractional-quantity V2?
5. **Cyrillic↔Latin search mechanism (A17).** Shadow column vs term expansion vs SQL collation.
6. **List pagination (A18).** In the current MVP slice, or v2?
7. **Sale COGS snapshot (A8/F5).** Snapshot per sale line for dispute-grade reconstructability, or accept v2 Reports deferral?
8. **Tenancy fail-open vs fail-closed (A1/F6).** Throw on authenticated org-0?

---

## Fixable items log (read-only session — NOT applied)

Small, self-contained; safe to batch in a cleanup pass. F-numbers cross-ref above.
- **F1** `TransactionService.cs:466-481` — pre-group request lines by `ProductId` before the refund cap. *(correctness — do first)*
- **F2** `CreateTransactionValidator.cs` — `RuleForEach(Lines)`: `Discount ≥ 0` (and `≤ 100` for Percentage). *(correctness)*
- **F3** wire or drop `Transactions.SearchTerm`, `Payments.SearchTerm`/`TransactionId`.
- **F4** `TransferLineConfiguration.cs:25` & `TransactionLineConfiguration` — `HasQuantityPrecision()` + migration.
- **F7** `PartnerService.cs:238` — reuse `PartnerBalance.Total`.
- **F9** delete/realign orphan `AddressDto`.
- **F10** align Product delete to 409.
- **A7** enrich insufficient-stock 400 with available-vs-requested (`StockMovementExtensions.cs:74`); prune stale `QuantityInStock` XML docs + dead `WithQuantityInStock` builder.
- **A9** ~~correct stale audit-findings WriteOff note~~ — moot: the audit doc was retired 2026-07-14.

---

## Out-of-band open items

Open items that live outside the A-row gap matrix (the list `CLAUDE.md` → Live trackers points at). Added 2026-07-14.

### 🔴 Committed-secrets rotation — open

Real-looking production secrets were committed to the repo and shared across environments (audit 2026-06-18 Part 4 — doc retired 2026-07-14, recoverable from Ombor.API git history): the SQL Server connection string with credentials in `appsettings.Production.json`, the JWT signing key, the Eskiz SMS token, a Sentry DSN — plus a plaintext SA password in `docker-compose.yaml`. Re-checked 2026-07-14: the Production config values are now blank and `Ombor.API.csproj` carries a `UserSecretsId` (the move is underway), but `docker-compose.yaml:16,24` still holds the plaintext SA password, and everything previously committed remains in git history. **Action:** rotate every exposed secret (DB credentials, JWT signing key, Eskiz token, Sentry DSN, SA password) and keep them exclusively in user-secrets/env vars.

### Error-key casing drift — open

Validation 400s serve `Errors` keys straight from FluentValidation `PropertyName`s — PascalCase, e.g. `Lines[0].Quantity` — while the rest of the JSON contract is camelCase (`backend-conventions.md` → Validation & error shape). Needs a ruling: keep PascalCase and document it as the contract, or normalize keys to camelCase. Until ruled, nothing may depend on `Errors`-key casing.

---

## Incoming contract gaps (harvested from retired `Ombor.Web/docs/backend-deltas.md`, 2026-07-14)

The frontend→backend delta queue is retired; its four surviving items were re-verified 2026-07-14 against `../../Ombor.Web/docs/openapi.json` and `src/` — three had already shipped, one carries over as open. **NEW frontend→backend gaps are now recorded as F-items in `../../Ombor.Web/docs/frontend-gaps.md`** (nothing appends to the retired queue).

### Open

**D1 · `TemplateDto` lacks `lastUsedAt` (was backend-deltas §6a)**
- **Contract:** `TemplateDto` (openapi ~8366) = id / partnerId / partnerName / name / type / items — no `lastUsedAt`. (The rest of old §6 shipped: `TemplateItemDto` now serves `sku`, `measurement`, `discountType`, and a decimal `quantity`.)
- **Symptom:** the Templates list's «Использован» column shows «—» on every row; New Sale/Supply's template-load is supposed to stamp the date.
- **Required:** add nullable date-time `lastUsedAt` to `TemplateDto`, stamped when a template is loaded into a transaction.

### Verified resolved at harvest (not carried as open)

- **§9 · default-partner seeding (F-013 + F-027)** — the "seed it as **Both**" branch of the decision shipped: `OrganizationSetupService.cs:38-44` seeds the starter partner («Розничный покупатель», language-matched) as `PartnerType.Both`, an ordinary row with no system flag (rule 42; cross-ref A19).
- **§10 · invalid enum → 400, not 500 (F-017)** — `ValidatingStringEnumConverter` (`Ombor.Contracts/Serialization/`) throws `InvalidEnumValueException` on an unparseable value; `InvalidEnumExceptionHandler` maps it to 400 `ValidationProblemDetails` (registered second in the handler chain, `DependencyInjection.cs:56`).
- **§12 · payment tenancy (F-029, suspected)** — `Payment : IOrganizationScoped` (`Payment.cs:6`) → covered by the reflection-applied global filter + stamping (A1); the dedicated two-tenant test §12 asked for exists: `Endpoints/OrganizationScoping/PaymentScopingTests.cs` proves payment/component/allocation isolation (plus Wallet and PartnerBalance scoping tests in the same folder).

---

## Part C — capability snapshot (what exists today)

One line per module; the "what exists" baseline for v2 planning. No judgments.

| Module | Can do today | Notable absences |
|---|---|---|
| **Products** | CRUD + multipart image upload/delete; filtered/searched list (term, category, price range, type, archived); soft archive/restore; reference-gated hard delete; per-product transaction history + warehouse movement ledger w/ running balance | No bulk import/export, no stock-take on this controller, no low-stock/valuation report, no price-history, no barcode/SKU lookup |
| **Partners** | CRUD (customer/supplier); search; soft archive/restore; reference-gated hard delete; server-computed net balance; full newest-first ledger (opening + txns + settling payments) w/ running balance | No statement export, no credit-limit/aging endpoint, no partner-scoped txn/order list beyond ledger, no bulk import/merge |
| **Categories** | CRUD; optional search; product-count per row; 409-gated hard delete | No hierarchy/subcategories, no archive, no reassign-on-delete, no bulk/reorder |
| **Transactions** | Create Sale/Supply/refunds (multi-line, stock movement, attachments, inline settling payment vs wallet); list (partner/status/type), detail, lines, per-txn payments; all totals/unpaid/overdue server-computed | No update/void/delete (refund counter-events only), no draft/quote/approval, no invoice export |
| **Payments** | Standalone payment (wallet source + oldest-first settlement, excess→advance per rule 40); payroll; list (partner/employee/date/type/dir), by-id, per-partner outstanding, form-data feed | No edit/reversal/delete (immutable), no split-tender/non-wallet source, no receipt, no advance-drawdown/refund |
| **Wallets** | Create/rename (type+opening immutable)/archive/restore; list/get w/ computed balance; atomic inter-wallet transfers (overdraw-blocked); per-wallet ops ledger + transfer history | No standalone deposit/withdrawal, no transfer reversal, `advancesHeld` hardcoded 0 |
| **Warehouses + Adjustments** | Warehouse CRUD + archive/restore + 409-gated delete; per-warehouse stock w/ WAC/value; one-time opening stock; adjustments (immutable ±, rule-20 blocked, atomic) + filterable list w/ running balance | No cycle-count/physical-inventory, no adjustment reversal, no bin/lot/expiry, no reorder min/max, no bulk import |
| **Transfers** | Create multi-line inter-warehouse transfer (atomic, rule-20 blocked, dest at source WAC); list (by warehouse); get-by-id | No lifecycle (draft/in-transit/partial receipt/accept-reject), no cancel/reversal, no cross-org |
| **Movements / Inventory** | Computed newest-first movement ledger per warehouse + per product (signed qty, counterparty links, running balances reconciling to on-hand) | Read-only; no date/kind filter or pagination, no cross-warehouse valuation/low-stock/aging, no export |
| **Templates** | Full CRUD on partner+line-item baskets; list filter by name/type | **No endpoint to instantiate a template into an Order/Sale** (stored, never applied); no soft-archive (hard delete) |
| **Orders** | Create/list/get/update + status machine (process/ship/reject/cancel/deliver/return); deliver enforces rule 20, moves stock, promotes to Sale | No partial/split delivery or line-level fulfillment, no order deposits/invoice, **Return is a bare status flip** (no refund/stock-back) |
| **Employees** | Full CRUD + search (name/position/phone); per-employee payrolls list | **Hard-deleted, not soft-archived** (unlike Product/Partner/Wallet); no attendance, no salary-change history, no balance-owed view |
| **Payroll** | Immutable Payroll payment per employee (salary snapshotted, expensed from one wallet); payroll history list | No batch/period run, no deductions/bonuses/tax/overtime, no accrual/owed-vs-paid, no edit/void |
| **Debts** | Single `GET /api/debts`: all outstanding debts derived from unpaid/partial txns — direction, remaining, age, overdue days, newest-first | No per-partner drill-down, no filtering, no manual entry, no export |
| **Dashboard** | Single `GET /api/dashboard?period=today\|week\|month`: revenue KPI + PoP delta, receivable/payable/overdue KPIs, bucketed time series per wallet, aging buckets, top-5 debtors, 10 recent txns | No custom date range, no inventory/margin KPIs, no configurable widgets, no export |
| **Auth/Org/Settings** | Phone register + SMS-OTP, login, refresh (httpOnly cookie), logout/revoke, forgot/reset; org profile get/update (logo upload); user list/invite(phone)/deactivate/reactivate; per-user language | No RBAC, no email/link invites, no self-service profile/password change, no user detail/edit, no audit/activity view |
