# Backend Complexity Notes — Ombor

> **Purpose.** The companion to [`backend-contract.md`](./backend-contract.md). The contract doc says *what shapes go in and out*; this doc explains the **non-obvious server-side logic** each endpoint hides — the things a mock fakes that the real backend must actually compute. Every "self-contained mock" limitation in the codebase is really an item on this list.
>
> Canon authority: `docs/canon/business-rules.md` (rule numbers below reference it). Where this doc and canon conflict, canon wins — raise it.

---

## A. Cross-cutting invariants (apply everywhere)

### A1. Multi-tenancy scoping
Every tenant-scoped entity (everything except auth/system) must be filtered by `TenantId`. Today only `User`/`Role` carry `OrganizationId`. Target: a `Tenant` entity + `TenantId` on all business entities, enforced by a **shared mechanism (EF Core global query filter)** so no query can leak another tenant's rows. This is the single highest-risk correctness concern — get it wrong and a partner sees another business's debts.

### A2. Everything is server-computed (rules 8 + 12)
The frontend **never** derives balances, totals, ages, counts, running balances, or WAC. It displays what you serve. That means:
- No stored balance columns that can drift — compute from events on read (or maintain a reconciled projection).
- Partner balance, wallet balance, "our money", advances, product `totalStock`/`averageCost`, debt `remaining`/`age`/`overdue`, every `balanceAfter` — all computed by you.
- A disputed figure must be **fully explainable from the event ledger**. This is the product's core differentiator (dispute-grade audit trail), so favor an event-sourced / append-only ledger over mutable balance fields.

### A3. Immutable events get no edit/delete (rule 1)
Transactions, payments, payroll, stock adjustments, transfers, opening balances, opening stock — **created once, never mutated**. Corrections are **counter-events** (refund, reverse payment), never UPDATE/DELETE. The frontend exposes no edit/delete affordances for these; the backend must reject such mutations even if a request is crafted.

### A4. Soft-archive, never hard-delete (rules 29–32)
`Product`, `Partner`, `Wallet`, `Warehouse` carry `IsDeleted`/`IsArchived`. Archiving is **never blocked by references**. **Archived rows with residual money/stock still count in aggregate totals** (rule 31) — an archived warehouse holding stock still contributes to stock value; an archived wallet holding cash still contributes to the global balance. The only true DELETE is the **reference-gated** one (Partners, Categories) → **409** when referenced.

### A5. Seeded defaults at tenant provisioning (rule 42 + decision 4.2)
On tenant setup, seed **ordinary** rows: one Cash `Wallet`, one `Warehouse`, one `Category`, **and one `Partner`**. They are normal entities — editable, archivable, deletable-when-unreferenced. **No `isSystem`/type-gating, no protected «Розничный покупатель».** The default partner exists only so the **required, non-null** `Transaction.partnerId` is always satisfiable (the walk-in case). A user who never needs partners reuses the default forever; one who does creates more and may delete the default once unused. *(This supersedes the system-partner entry still in `tech-change-list.md`.)*

### A6. Audit log (rules 26–28)
A single audit table fed by an **EF Core interceptor**: master-data CRUD (Product, Partner, Wallet, Warehouse, Employee, Template, Order) + every money/stock event, recording **actor, timestamp, entity type + id, event type, before/after values**. One Activity Log screen reads it. *(The screen is deferred to v2, but the capture should be built in from the start — retrofitting audit onto existing writes is painful.)*

### A7. Cyrillic↔Latin search
Name/SKU search must match across scripts transparently (a user typing Latin finds a Cyrillic-named product and vice-versa). The frontend has a client-side transliteration helper for the mocks; once lists move server-side this must live in the query layer.

---

## B. The WAC (weighted-average cost) engine — the hardest piece

Stock value uses **weighted-average cost per product per warehouse** (`InventoryItem.AverageCost`), rules 17–25. This is the backbone of inventory correctness and touches many endpoints.

**Stock-IN events recompute WAC** (new average = `(oldQty·oldWAC + inQty·inCost) / (oldQty + inQty)`):
- Supply transaction line
- Opening stock (`/warehouses/{id}/opening-stock`)
- Transfer **into** the destination warehouse (carries the source's WAC, not a new cost)
- Increase stock-adjustment (restores at **current** WAC — does **not** change it)
- Sale refund back into stock

**Stock-OUT events leave at current WAC** (do not change the average):
- Sale transaction line → COGS at WAC
- Transfer **out** of the source warehouse
- Decrease stock-adjustment → records the loss **at WAC, as a loss line distinct from COGS** (rule 23)
- Supply refund out of stock

**Hard rules:**
- **Negative stock is impossible** (rule 20): Sale lines, Decrease adjustments, Transfer lines, and Order delivery are all hard-blocked when they'd drive stock below zero. Return **400 ValidationProblemDetails** with the available-vs-requested detail.
- **Atomicity:** a Transfer updates **both** warehouses in one transaction; a Sale/Supply updates stock **and** money **and** ledgers atomically. Partial application must be impossible.
- **Base-unit movement:** stock moves in base units; package count is retained on lines (rules 19, 21).
- `Product.totalStock` = Σ inventoryItems.quantity; `Product.averageCost` = value-weighted across warehouses; per-warehouse WAC lives on each `inventoryItem`.

The mocks **derive** Warehouses 1–2 stock from the Products seed but do **not** mutate stock on any write. So **all** of the above is unimplemented today — it's the core of the backend job.

---

## C. The two payment models (resolve before building Payments)

The frontend currently holds **two** payment type families in `src/models/payment.ts`:

1. **Legacy** — `PaymentMethod` (Cash/Card/BankTransfer/AccountBalance) + `currency` + `exchangeRate`, allocation types `Sale|Supply|SaleRefund|SupplyRefund|AdvancePayment|ChangeReturn`. Still referenced by **Payroll** and the **New-Sale inline debt-payment** flow.
2. **Redesigned** (`PaymentRecord`) — the target: `PaymentSource` (`Wallet`/`Advance`) + `PaymentAllocationEntry` (`TransactionSettlement`/`AdvanceCredit`/`ChangeReturn`).

**Build the redesigned model.** It is the canonical one (rules 8–14). The legacy types are transitional scaffolding the frontend will shed; do not implement `currency`/`exchangeRate`/`method` (UZS-only, rule 33). Payroll should become a `PaymentRecord` of `type: Payroll` flowing through a wallet.

### C1. Source = allocation invariant (rule 8)
A payment's **sources** (where the money came from: one or more wallets, and/or a partner's advance) must **sum to its settling allocations** (what it paid off: transaction settlements + advance credit). This is the dispute-grade guarantee: every som is traceable from a wallet/advance to the transaction it settled.

### C2. ChangeReturn sits outside the identity (rule 11/14)
`ChangeReturn` (giving change back on an overpayment) is a **memo**, excluded from the source=allocation balance and from wallet/partner balance effects. The wallet shows **net** of change. Don't let it leak into the invariant.

### C3. Advance is a claim, not a location (rule 11)
A partner **advance** is a liability claim, not a physical pot of cash. The cash itself sits in a wallet (`Wallet.advancesHeld` tracks how much of a wallet's balance is actually partner money). `Wallet.ourMoney = balance − advancesHeld` (rule 12). When a partner pays in advance: wallet balance ↑, `advancesHeld` ↑, `ourMoney` unchanged.

### C4. Advance credit gated on zero debt (rule 40)
You can only **create** a partner advance (overpayment → advance) when the partner has **no outstanding debt**. If they owe, the excess must first settle the debt; only the remainder past zero debt may become an advance. The New-Sale overpayment toggle (Сдача/Аванс) and the standalone payment both depend on this.

### C5. Settlement & the `outstanding` endpoint (rule B / FIFO)
`GET /api/payments/outstanding?partnerId=` returns the partner's open transactions oldest-first. The settlement UI offers: auto-allocate chronologically (FIFO), manual per-row, or leave-as-advance. `CreatePaymentRecordRequest.settlements[]` carries the chosen per-transaction amounts; **excess beyond settlements becomes an AdvanceCredit** (subject to C4). The backend validates each settlement ≤ that transaction's remaining.

---

## D. Transaction creation (`POST /api/transactions`, JSON) — the heaviest orchestration

The New Sale / New Supply POST is a **single atomic operation that does many things**. The mock fakes all of it (joins the feed, computes totals, mutates nothing). The real backend must, in one transaction:

1. Create the Sale/Supply with its lines (resolving line totals from discount — see §F).
2. **Move stock** at the chosen warehouse — Sale decrements (at WAC, hard-blocked over-stock); Supply increments (recomputes WAC). Refund/opening handled elsewhere.
3. **Apply payment**: `paidAmount` from `walletId`, distributed across `settlements[]`, with `overpayment: "change"|"advance"` disposition (C2/C4).
4. **Update partner balance** (Sale ↑ receivable, Supply ↑ payable) and **wallet balance**.
5. Append ledger entries (partner ledger, wallet operations) and audit records.
6. Compute and return `totalDue`/`totalPaid`/`remaining`/`paymentStatus`.

`paymentStatus`: `paid` if `due−paid ≤ 0`, `partial` if `paid > 0`, else `unpaid`. **Direction asymmetry:** Sale prices from `salePrice` and **hard-blocks over-stock**; Supply prices from `supplyPrice` and **does not** stock-check (a supply adds stock). Balance projection flips sign by direction.

> The `content-type` branch (JSON → entry create; multipart → legacy passthrough) is a **mock transition shim**. The real backend only needs the JSON contract; the legacy multipart create can be dropped once the frontend's legacy flow is gone.

---

## E. Refunds (`POST /api/transactions/{id}/refund`) — rules 2–7

- **Linkage required:** refund references `originalTransactionId`; type must match (`SaleRefund` of a Sale, `SupplyRefund` of a Supply).
- **No refund-of-refund** (rule 4): reject if the original is itself a refund.
- **Mandatory reason** (rule 7).
- **Per-line cumulative cap** (rule 5): `Σ(already-refunded qty for this line) + requested ≤ original line qty`. Track cumulative refunds per original line across multiple partial refunds.
- **Effects** (mock does none): a SaleRefund returns stock (WAC recompute) and reduces the partner receivable / returns money; a SupplyRefund removes stock and adjusts the payable. Atomic, immutable.

---

## F. Discount resolution (decision 4.4) — money-precise

Each line carries `discount` + `discountType`:
- `discountType = "pct"` → line discount amount = `unitPrice × quantity × discount / 100`.
- `discountType = "fixed"` → line discount amount = `discount` **UZS off the whole line** (not per unit). Clamp to the line gross (a fixed discount can't exceed the line).
- **Line total** = `unitPrice × quantity − lineDiscountAmount`.
- **Transaction total** = Σ line totals. The "total discount" shown in the UI = Σ line discount amounts — **computed, never stored as its own field, never an input.**
- **Persist both `discount` and `discountType`** end-to-end (no converting fixed→percentage). This is the decision that prevents precision loss and prevents a fixed amount from rescaling when a mutable line (Order/Template) is later re-priced.
- ⚠ **Templates** today store only `discount` as a percentage with no `discountType`; add `discountType` there too so a fixed discount survives template→transaction load.

---

## G. Partner balance & ledger — sign convention + reconciliation

- **Sign:** `balance > 0` = **partner owes us** (receivable); `balance < 0` = **we owe the partner** (payable). Keep this consistent across Partner, Order `customerBalance`, Debt direction, and Dashboard.
- **Opening balance** is an immutable ledger event at creation (signed), never an editable field (rule 16). `UpdatePartnerRequest` deliberately omits it.
- **Ledger reconciliation:** `GET /partners/{id}/ledger` must produce a newest-first running balance whose final value **equals** `Partner.balance` exactly. Event types: opening, sale, supply, refund-sale, refund-supply, payment, deposit, withdraw. This ledger is what makes a disputed balance defensible payment-by-payment.
- **`isDeletable`** = true iff no other entity references the partner. DELETE on a referenced partner → **409**; the UI then steers to archive.

---

## H. Wallets — balance composition (rules 11, 12, 15, 16)

- `balance` = opening balance + wallet-sourced payment components (in/out) + inter-wallet transfers (in/out).
- `advancesHeld` = sum of partner advances physically held in this wallet.
- `ourMoney` = `balance − advancesHeld`.
- **Opening balance + type are immutable** after creation (rule 16); `UpdateWalletRequest` is name-only.
- **Inter-wallet transfer** (`POST /api/wallets/transfers`): atomic, immutable, hard-blocked over the from-wallet balance, appends one operation row to **each** side. Both wallets' balances move in one transaction.
- The mock's `balanceAfter` running balances are illustrative; the real backend must make them reconcile to the opening balance.

---

## I. Orders — state machine + delivery promotion (rules + decision 4.5)

**State machine** (the only **mutable** transaction — a requested intent before money/stock move):
```
Pending → Processing → Shipping → Delivered → (Returned)
   └──────┴───────────┴─→ Cancelled / Rejected   (pre-delivery only)
```
Each transition appends an `OrderStatusEvent {at, from, to, by}`. Validate legal transitions (e.g. can't ship a Pending order, can't cancel a Delivered one).

- **No stock reservation while Pending** (decision 4.5). `warehouseId` is a **nullable intended warehouse** — pre-fills the delivery dialog, expresses intent, but reserves nothing.
- **`PUT` warehouse semantics:** `undefined` = keep existing, `null` = clear, value = set. (The mock distinguishes these three; the backend must too.)
- **Deliver** (`POST /{id}/deliver`, `{ warehouseId }`): requires a warehouse, runs the **hard stock check** (rule 20) at that warehouse, then **promotes the order to a Sale** — sets `status=Delivered`, `saleId`, appends history. The real promotion must **actually create the Sale transaction** (decrement stock at WAC, create the receivable/settlement) — the mock only assigns a `saleId` and writes nothing. Promotion must **fail cleanly** (400) on insufficient stock.

---

## J. Debts — derived read model (no stored entity)

`GET /api/debts` is **not** a table. It is a projection over unpaid/partially-paid transactions:
- One row per outstanding transaction; `remaining = total − paid`.
- `direction`: Receivable (unpaid Sale / SupplyRefund — they owe us) vs Payable (unpaid Supply / SaleRefund — we owe).
- `ageDays` = today − transaction date; `overdueDays` = today − dueDate (>0 = overdue). `dueDate` = transaction date + payment terms.
- Aggregations (summary cards, by-partner groups, aging buckets) are computed client-side from this list — so the **list must be complete and consistent** with transactions.
- Partner ids must match real partners (the UI deep-links debt rows to `/partners/:id`).

---

## K. Dashboard — aggregated read model + a deliberate definition divergence

`GET /api/dashboard?period=` returns a snapshot:
- **`period`** (`today`/`week`/`month`) drives only the **revenue KPI and the two time-series charts**. The debt KPIs (receivable/payable/overdue/aging/top-debtors) are a **current snapshot, period-independent**.
- **Debt figures must reconcile with `/api/debts`** — compute both from the same source (mvp-plan §2 Done criterion). If the two screens disagree, it's a bug.
- ⚠ **Definition divergence to preserve:** the dashboard's **«Просрочено» = receivables aged 31+ days** (the aging-bucket definition), which is **different** from `/debts` **past-due-date** overdue. Against the current date the dashboard can read 0 overdue while `/debts` shows due-date-overdue rows. This is intentional and faithful to the prototype — do **not** "fix" it by unifying the two definitions.
- `series` carries per-wallet payin/payout splits (`walletPayin[]`/`walletPayout[]` aligned to `wallets[]`) for the payments chart's wallet filter.

---

## L. Number generation & references

- **Transaction number**: next sequential per type.
- **Refund number**: `{originalNumber}-R{seq}` where seq = (# existing refunds of the original) + 1.
- **Payment number**: human-friendly `P-###`.
- **Order number**: human-friendly order code.
- The mocks generate these as max-seed+1; the backend needs real per-tenant sequences (and they should be tenant-scoped, gapless enough to be defensible in a dispute but not necessarily strictly contiguous).

---

## M. Things the mocks fake that are NOT backend work (don't be misled)

A few mock behaviors are **transition shims**, not target backend logic:
- The `content-type` branch on `POST /api/transactions` (JSON vs multipart) — only the JSON contract is the target.
- The password-reset **demo code `1234`** — replace with real SMS/OTP issuance + verification.
- `GET /api/partners/{id}/payments` returning `[]` — legacy stub, superseded by the ledger; can be dropped.
- `logoUrl` as a data URL in Settings — the real backend stores/serves an uploaded file URL.
- Wallet/payment numbers and `balanceAfter` being "illustrative" in some mocks — the real backend must make them reconcile.

---

## N. Open canon edits (yours to apply — `docs/canon` is read-only to the agent)

1. `tech-change-list.md`: rewrite the **system-partner** entry → "seed an ordinary default partner" (matches rule 42's seeded defaults; no `isSystem`).
2. `business-rules.md` rule 37: clarify **`Fixed` discount = per-line currency (whole line), not per-unit**.
3. `business-rules.md` Employee/Payroll + `tech-change-list.md`: confirm **payroll PUT/DELETE removed**, multi-per-month allowed, payroll on the redesigned Payment model (no currency/method enum).
4. Order warehouse wording is already updated to "warehouse at creation as intended, no reservation, authoritative at delivery" — verify it matches decision 4.5.
