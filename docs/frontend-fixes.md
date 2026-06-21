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
