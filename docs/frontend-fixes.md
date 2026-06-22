# Frontend fixes — backend-driven

Running list of changes the **frontend** must make to match the redesigned backend. These are
backend/frontend contract mismatches discovered while building the backend; the backend is the
source of truth here (it conforms to `business-rules.md` and the corrected `backend-contract.md`),
so the frontend adapts. Apply these once the backend redesign is complete and the frontend is
re-pointed at the new API.

Each entry: **what the frontend does today → what it must do**, with the backend reason.

---

## Transactions — `POST /api/transactions`

- **Discount type values.** Frontend sends per-line `discountType` as `"pct"` / `"fixed"`.
  Backend expects the canonical enum names **`"Percentage"` / `"Fixed"`** (case-insensitive).
  *Reason:* the backend enum is `DiscountType { Percentage, Fixed }`; short acronyms are not used
  anywhere in the codebase. The frontend must send the full names. (Discovered M2c.)

- **Content type.** Frontend sends the create request as JSON. Backend expects
  **`multipart/form-data`** (transactions carry file attachments). *Reason:* attachments require a
  multipart body; the contract already specifies multipart. (Build-plan settled decision.)

- **Request shape / refunds.** Frontend uses `direction: "Sale" | "Supply"` plus a separate
  `CreateRefundRequest` / refund endpoint. Backend is a **single `POST /api/transactions`** with
  `type` ∈ `{ Sale, Supply, SaleRefund, SupplyRefund }` (refunds carry `originalTransactionId` and
  a required `refundReason` on the same request, not a separate `CreateRefundRequest.reason`).
  *Reason:* one immutable create path for all four types (build-plan settled decision; no separate
  `/refund` endpoint).

- **Payment fields.** Frontend's legacy create payload carried `payments[]` / `debtPayments[]` /
  `shouldReturnChange`. Backend now takes a single wallet source: **`walletId`, `paidAmount`,
  `settlements[]` (`{ transactionId, amount }`), `overpayment` (`"change" | "advance"`)**.
  *Reason:* the redesigned source/allocation payment model (rules 8–12, 40). (Discovered M2c.)

- 🟢 **Transaction detail — `GET /api/transactions/{id}`** (200/404). Now returns a full
  `TransactionDetail` for the sale/supply detail page, in **one call** (no need to combine `/lines` +
  `/payments`): `{ id, number, type, direction, status, date, dueDate, partnerId, partnerName,
  partnerCompany, partnerType, warehouseId, warehouseName, totalDue, totalPaid, remaining, lines[],
  payments[], attachments[], createdBy, notes, originalTransactionId, refundReason }`. `lines[]` = the
  `TransactionLine` shape; `payments[]` = the same settlement shape as `GET /{id}/payments` (newest-first).
  `number`/`direction` match the **debts** read model (provisional `S-`/`SP-`/`SR-`/`SPR-` number;
  `direction` is the money direction `Receivable`/`Payable`, **not** the sale/supply route — use `type`
  for routing). This **supersedes `GET /{id}/lines`** for the detail page (that endpoint stays for now but
  returns lines only).
  - **`createdBy`** is the author's **display name** (audit card), or `null` for seed/system rows — note
    this is a name, unlike the raw user-id `createdBy` on `Wallet`/`Transfer`/`StockAdjustment` today.
  - **`notes`** is the transaction's free-text note (`null` when none) — the same `notes` field the create
    form already sends; it's now persisted on the transaction and returned here.
  - **`attachments[]`** = `{ name, contentType, sizeBytes, url }` — **raw values only**: the frontend
    derives the pdf/img icon from `contentType` and formats `sizeBytes` (e.g. «248 КБ»). Files are uploaded
    with the existing multipart create (`Attachments` field, already sent) — no request change, they're just
    persisted and served now.

## Payroll — `POST /api/employees/{id}/payrolls`

- **Request shape.** Frontend's legacy payroll create sent `amount`, `currency`, `exchangeRate`,
  `method`. Backend now takes **`employeeId`, `walletId`, `amount`, `period` (e.g. `"2026-06"`),
  `notes`** — UZS-only, drawn from a wallet (rule 9), no currency/rate/method. *(Discovered M2d.)*

- **No payroll edit/delete.** Payroll is **immutable** — there are no PUT/DELETE endpoints; a
  mistaken payroll is corrected with a reverse payment, not an edit. The frontend must not offer
  edit/delete on a payroll record. *(business-rules rule 1.)*

- **Response shape.** Payroll create + list now return the redesigned `PaymentRecord`
  (`number`, `period`, `salary`, `employeePosition`, `sources[]`), not the legacy payment shape.

## Removed / reshaped legacy payment endpoints (M2f)

- **`GET /api/partners/{id}/payments` removed.** It returned the legacy payment shape and is
  superseded by the partner **ledger** (coming in M3). The frontend must stop calling it; use the
  ledger for per-partner history.

- **`GET /api/transactions/{id}/payments` reshaped.** It no longer returns `currency`/`method`
  (legacy). Each line is now `{ id, transactionId, amount, paymentNumber, walletName, walletType,
  notes, date }`. The frontend's `TransactionPaymentLine.method` no longer exists — show the wallet
  (name/type) instead.

- 🟢 **Payment allocation rows now carry the transaction type.** On `PaymentRecord` (`GET /api/payments`,
  `GET /api/payments/{id}`), each `allocations[]` entry adds **`transactionType`** ∈
  `Sale|Supply|SaleRefund|SupplyRefund`, **null** for `AdvanceCredit`/`ChangeReturn` (no transaction).
  `transactionId` alone can't pick the detail route since it's split (`/sales/:id` vs `/supplies/:id`);
  `transactionType` resolves it (map `SaleRefund`→sales, `SupplyRefund`→supplies) and also supplies the
  detail page's `direction` prop. Settlement rows become clickable; advance/change rows stay plain text.

## Partners (M3)

- **Opening balance, not balance.** `CreatePartnerRequest` takes **`openingBalance`** (signed:
  +partner owes us), not `balance`. `UpdatePartnerRequest` **has no balance field at all** — the
  opening balance is an immutable event set once at creation.

- **Partner read shape.** `PartnerDto` no longer carries the legacy `balanceDto` breakdown. It now
  exposes `balance` (computed net), `openingBalance`, `openingDate`, `isArchived`, `isDeletable`,
  `activityCount`.

- **Ledger endpoint.** `GET /api/partners/{id}/ledger` returns `PartnerLedgerEntry[]` newest-first
  with a running `balance` that reconciles to the partner's net balance. Entries omit the contract's
  legacy `method`/`allocation` fields (method is gone with the legacy payment model).

- 🟢 **Ledger entries now carry the wallet.** `PartnerLedgerEntry` adds **`walletName`** / **`walletType`**,
  populated only on **payment** rows (`type ∈ payment|deposit|withdraw`) and **null** on `opening` and
  transaction rows (a transaction isn't tied to a single wallet — it's settled across zero-to-many payments).
  Use them for the wallet column on the partner **«Платежи»** tab (which filters the ledger to payment rows).

## Products (M4a)

- **No `quantityInStock`.** `Create/UpdateProductRequest` no longer take `quantityInStock` — products
  are **created at zero stock** (rule 17); stock arrives via opening-stock / supply. `ProductDto` no
  longer returns `quantityInStock`.

- **Computed stock fields.** `ProductDto` now exposes `totalStock` (sum across warehouses), `averageCost`
  (value-weighted; **null** when there's no stock), `isLowStock` (from `totalStock`), `isArchived`, and
  `inventoryItems[]` reshaped to `{ inventoryId, inventoryName, quantity, averageCost }`.

## Orders (M5a)

- **Discount type values.** Same as transactions: order line `discountType` must be the canonical
  **`"Percentage"` / `"Fixed"`** (case-insensitive), not `"pct"` / `"fixed"`. *Reason:* shared
  `DiscountType` enum; no acronyms.

- **`deliveryAddress` is a string.** The order's `deliveryAddress` is now a **free-text string**
  (nullable), not a `{ latitude, longitude }` object. Coordinates are retained server-side but
  dormant (not in the contract); the frontend sends/receives a plain address string.

- **Order total field.** `OrderDto` exposes the total as **`total`** (was `totalAmount`). Lines carry
  computed `total`, plus `sku` and `measurement` (from the product) and `discountType`.

- **Status endpoints return the order.** `POST /api/orders/{id}/{process|ship|deliver|cancel|reject|return}`
  now return **`200` + the full `OrderDto`** (were `204 No Content`). Illegal transitions return **`409`**.

- **`PUT /api/orders/{id}` warehouse tri-state.** `warehouseId` is tri-state: **omit = keep**,
  **`null` = clear**, **value = set**. The frontend must omit the field (not send `null`) when it
  means "leave unchanged". Editing is only allowed while the order is open (Pending/Processing).

- **New read fields.** `OrderDto` adds `customerType`, `customerBalance` (computed), `warehouseId` /
  `warehouseName` (intended warehouse), `saleId` (set once delivered — M5b), and `history[]`
  (`{ at, from, to, by }`, oldest-first).

## Warehouses (M4b) — the `Inventory`→`Warehouse` rename

- **Resource cutover.** `/api/inventories` is **gone**; use **`/api/warehouses`** (hard cutover, no
  alias). *Reason:* the stock-location entity is canonically a **Warehouse** (a place); "inventory" is
  the stock it holds. Every `inventoryId`/`inventoryName` field across the API is now
  **`warehouseId`/`warehouseName`** — including `CreateTransactionRequest` (`warehouseId`, was
  `inventoryId`) and the product DTOs (`ProductDto.warehouseItems`, each `{ warehouseId, warehouseName, … }`).

- **Warehouse shape.** `WarehouseDto` = `{ id, name, location, productCount, totalUnits, stockValue, isArchived }`
  — **computed totals**, no embedded item list, no `isActive`. The per-product stock moved to
  **`GET /api/warehouses/{id}/stock`** → `WarehouseStockItem[]` (`{ productId, productName, sku,
  categoryName, measurement, quantity, averageCost, value }`).

- **Create/Update.** `CreateWarehouseRequest { name, location }` (no `isActive`); **duplicate name → 400**.
  `UpdateWarehouseRequest { id, name, location }`.

- **Archive, not delete.** The hard `DELETE /api/inventories/{id}` is **removed**. Warehouses are
  soft-archived: **`POST /api/warehouses/{id}/archive`** / **`/restore`** (both return the `WarehouseDto`,
  200). Archived warehouses still appear in `GET /api/warehouses` and still count in totals (rule 31).

## Stock Adjustments (M4c) — now REAL (was mocked)

- **Live endpoints.** `GET /api/stock-adjustments` (newest-first; optional `?warehouseId=&productId=`) and
  `POST /api/stock-adjustments` (201). Immutable — **no edit/delete** (corrections are counter-adjustments).
- **Request:** `{ warehouseId, productId, direction: "Increase"|"Decrease", quantity, reason, note? }`.
  `reason` must be from the set for the direction (Decrease: `Damage|Expiry|Theft|RecountDown|Other`;
  Increase: `Found|RecountUp|Other`) — a mismatch is **400**. A Decrease over available stock is **400**.
- **Response** `StockAdjustmentDto`: `{ id, date, warehouseId, warehouseName, productId, productName, sku,
  categoryName, measurement, direction, quantity, reason, note, createdBy, balanceAfter }`. **`balanceAfter`**
  is the product's stock in that warehouse **right after** the adjustment (the point-in-time ledger balance —
  a historical row keeps its own value, it is not the current stock).

## Transfers (M4d) — DTO reshaped

- **`TransferDto`** drops **`status`** (transfers are immutable single-step) and adds **`createdBy`**; the
  date field is **`date`** (was `dateUtc`) and the note field is **`note`** (was `notes`). **`TransferLine`**
  now carries **`sku`** + **`measurement`**.
- **`CreateTransferRequest`** = `{ fromWarehouseId, toWarehouseId, note, lines: { productId, quantity }[] }`
  (note: **`note`**, was `notes`).
- **Immutable** — no PUT/DELETE. `GET /api/transfers` (newest-first, optional `?warehouseId=`) + `POST` only;
  each line is hard-blocked over the source stock (400), and both warehouses move atomically.

## Stock movements (M4e) — new read models

- **`GET /api/warehouses/{id}/movements`** → `WarehouseMovement[]` (newest-first):
  `{ id, date, kind, productId, productName, measurement, counterparty, note, quantity, balanceAfter }`.
  `kind ∈ Opening | Supply | Sale | Refund | Adjustment | Transfer`; `quantity` is **signed** (+ in, − out);
  `balanceAfter` is the product's running stock **in this warehouse**. `counterparty` = the partner (sale/
  supply/refund) or the other warehouse (transfer); `null` otherwise.
- **`GET /api/products/{id}/movements`** → `ProductMovement[]` (newest-first):
  `{ id, productId, date, kind, warehouseId, warehouseName, quantity, balanceAfter }`. `balanceAfter` is the
  product's running **total stock across warehouses** — a transfer appears as **two** rows (send at the source,
  receive at the destination) that net to zero there.
- 404 if the warehouse/product doesn't exist.

## Debts (M6a) — now REAL (was mocked)

- **`GET /api/debts`** → `Debt[]` (newest-first): `{ transactionId, number, direction, transactionType,
  partnerId, partnerName, partnerCompany, partnerType, date, dueDate, total, paid, remaining, ageDays, overdueDays }`.
  `direction ∈ "Receivable" | "Payable"`. Outstanding transactions only (`remaining > 0`).
- **`number` is provisional** — derived from type + id (`S-`/`SP-`/`SR-`/`SPR-`), not a stable per-type sequence
  yet (real transaction numbering is future work). Treat it as display-only; it may change later.
- **`dueDate` can be null.** A transaction's due date is now an **optional** field on create
  (`POST /api/transactions` accepts `dueDate`, a `yyyy-MM-dd` date); blank = due on receipt, so `overdueDays` is
  `0`. There is no "payment terms" concept — the operator sets the date directly.

## Dashboard (M6b) — now REAL (was mocked)

- **`GET /api/dashboard?period=today|week|month`** (default `month`, case-insensitive) → `DashboardData`. The
  response `period` echoes the lowercase token.
- **Wallet type has three values.** `wallets[].type` is **`"Cash" | "Card" | "Bank"`** (PascalCase, matching the
  rest of the API), not the contract's lowercase `"cash" | "bank"`. *Reason:* the model has three wallet types and
  collapsing `Card` into `bank` would lose information (user decision, M6b). The payments-chart filter must handle
  three values.
- **Series field names.** `series[]` = `{ label, sales, supplies, payin, payout, walletPayin[], walletPayout[] }`;
  `walletPayin`/`walletPayout` are per-wallet arrays **aligned to `wallets[]` order**.
- **«Просрочено» (overdue) = receivables aged 31+ days**, which is **distinct** from `/api/debts` due-date
  `overdueDays` — the two screens can legitimately disagree on "overdue" (intentional, complexity notes §K).
- **Debt figures reconcile with `/api/debts`** — receivable/payable/aging/top-debtors are derived from the same
  source, so the dashboard and the debts page always agree.

## Settings (M7) — now REAL (was mocked)

- **Org profile:** `GET/PUT /api/settings/organization` → `{ name, address, phone, email, logoUrl }`. **PUT is
  `multipart/form-data`** (`name`/`address`/`phone`/`email` text fields + optional `logo` file) — not JSON.
  Omitting the `logo` file keeps the existing logo; the response returns the hosted `logoUrl`.
- **Invite is phone-only in v1.** `POST /api/settings/users/invite` with `{ method, value }` accepts
  **`method: "phone"`** only; `method: "email"` returns **400** (login is phone-based). The invitee is created
  active with a random password and signs in via the OTP / forgot-password flow.
- **Per-user language endpoint** (not in the original contract): **`PUT /api/settings/language`** with
  `{ language }` ∈ `"ru" | "uz-Latn" | "uz-Cyrl"` sets the **current** user's language (the header globe). Stored
  on the user; invalid values → 400.
- **`TenantUser` shape served:** `{ id, name, contact, contactType: "phone", active, self, online, lastActiveAt }`.
  `online` is always `false` (no presence tracking in v1); `lastActiveAt` is the **deactivation date** (null while
  active); `self` marks the current user. Deactivated users remain in the list (rule 41) and **cannot log in**.
