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
**Status:** not started
**Depends on:** nothing — unblocks everything.
Mechanical, each independently shippable:
- Rename `Tenant`/`TenantId` → `Organization`/`OrganizationId` throughout (entity, claim, query filter, configs, migrations).
- Add `OrganizationId` + `ITenantScoped` to `PartnerBalance` (closes the tenant-scoping hole).
- Category→Products `OnDelete(Cascade)` → `Restrict`; add a 409 reference-gate on category DELETE; add `productCount` to `CategoryDto`.
- Fix `GET /api/payments/{id}` (currently throws `NotImplementedException`).
- Remove the dead `TransactionType.WriteOff` enum value and its references.
- Add `DiscountType` to `TransactionLine`, `OrderLine`, `TemplateItem` + recompute logic (rule 37). Persist `discount` + `discountType`.
- **Defer** `Product.QuantityInStock` removal to M4 (anything reading it must move to `InventoryItem` first).

**Decisions & notes:** _(Code fills at session end)_

### M1 — Wallets + org-setup seeding
**Status:** not started
**Depends on:** M0.
- `Wallet` entity + `WalletType` enum (Cash/Card/Bank), CRUD, archive/restore.
- Computed `balance` (opening + wallet-source components + transfers), `advancesHeld`, `ourMoney` (rule 12) — as tenant-scoped projections.
- Opening-balance as an immutable event; inter-wallet transfers (`POST /api/wallets/transfers`) — atomic, immutable, hard-blocked over from-balance, one operation row per side.
- `/wallets/{id}/operations`, `/wallets/{id}/transfers`.
- **Org-setup seeding (rule 42):** one Cash wallet, one warehouse, one category, one ordinary partner at organization provisioning — all ordinary entities, no system flag.

**Decisions & notes:**

### M2 — Payment field rework
**Status:** not started
**Depends on:** M1 (payments reference wallets).
- Update the payment model to the redesigned shape: sources `{Wallet, Advance}`, allocations `{TransactionSettlement, AdvanceCredit, ChangeReturn}`. Drop `PaymentMethod`/`Currency`/`ExchangeRate`; replace the legacy `PaymentAllocationType` values.
- Enforce the rule-8 identity (sources = settling allocations; ChangeReturn excluded), advance-as-claim (rule 11), advance gating on zero debt (rule 40), change-return-as-memo.
- Re-point Transactions, Refunds, and Payroll at the updated payment fields (relationship unchanged; field shape changes). Payroll drops legacy currency/method/rate.
- `GET /api/payments/form-data`, `GET /api/payments/outstanding?partnerId=` (FIFO oldest-first).

**Decisions & notes:**

### M3 — Partner balance projection + ledger
**Status:** not started
**Depends on:** M2.
- Rebuild `PartnerBalance` as the tenant-scoped projection derived from events, recomputed in-transaction (it's already `init`-only and not hand-mutated — extend, don't redesign the mutation model).
- `GET /api/partners/{id}/ledger` — derive-on-read from transactions + payments + opening balance; newest-first; running balance reconciles exactly to the stored balance.
- Add `openingBalance` (immutable event at creation, not editable), `isDeletable`, `activityCount`; 409 delete-gate when referenced.

**Decisions & notes:**

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
