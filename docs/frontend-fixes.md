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
