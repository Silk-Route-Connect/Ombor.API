# Backend Contract Reference — Ombor

> **Purpose.** This is the API contract the redesigned frontend already calls. It is **derived from the frontend models (`src/models/*.ts`) and the MSW mock handlers (`src/mocks/handlers/*.ts`)**, which were written as the real future v1 contract. Build the backend against this checklist, then the mocks get deleted.
>
> **Status legend per resource:**
> - **REAL** — a backend endpoint already exists and is used as-is.
> - **STALE** — a backend endpoint exists but its DTO/behavior is out of date; the frontend calls a target-shape contract instead.
> - **MOCKED** — no backend exists at all; the whole resource is invented at the target contract.
>
> **Companion doc:** [`backend-complexity-notes.md`](./backend-complexity-notes.md) explains the non-obvious server-side logic each endpoint hides (WAC, payment source=allocation, balance derivation, order promotion, read models). Read it alongside this.

---

## 0. Global conventions

These apply to **every** endpoint unless stated otherwise.

| Concern                                   | Contract                                                                                                                                                                                                                                                                                        |
| ----------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Base path**                             | All routes under `/api/`. Base URL = `VITE_OMBOR_API_BASE_URL`.                                                                                                                                                                                                                                 |
| **Auth**                                  | `Authorization: Bearer <accessToken>` on every non-auth request (interceptor-added). `withCredentials: true` (refresh-token cookie). Auth endpoints (`login`/`register`/`verification`/`refresh-token`/`logout`) need no bearer.                                                                |
| **401 handling**                          | A 401 triggers a single silent `refresh-token` retry; if that fails → logout. So the backend must return **401** (not 403) for expired/absent access tokens, and the refresh endpoint must rotate tokens.                                                                                       |
| **Error shape**                           | ASP.NET **`ProblemDetails`** `{ status, title, detail? }` for general errors; **`ValidationProblemDetails`** `{ status, title, errors: Record<string,string[]> }` for 400s. Field keys are dotted/indexed (`lines[0].quantity`). The frontend reads `errors` to show inline per-field messages. |
| **Validation status**                     | `400` validation, `404` not found, `409` reference-gated delete conflict, `201` create, `200` everything else.                                                                                                                                                                                  |
| **Money**                                 | Integer **UZS**. No decimals, no currency field on new contracts (UZS-only, business-rules §H).                                                                                                                                                                                                 |
| **Dates**                                 | ISO 8601 strings. `date` for event dates; `createdAt`/`lastActiveAt` for timestamps. Order delivery is split `deliveryDate` (`YYYY-MM-DD`) + `deliveryTime` (`HH:mm`).                                                                                                                          |
| **Lists**                                 | **No server-side pagination/sorting/filtering in v1** (decision 2026-06-11). List endpoints return the **full array**; the client filters/sorts/searches in memory. Future target is `PagedResponse<T>` — design DTOs so a paged wrapper can be added later without reshaping the row.          |
| **Server-computed fields (rules 8 + 12)** | Every balance, total, running balance, age, count, and WAC figure is **computed by the backend and returned in the DTO**. The client never recomputes them from event lists. These are marked **⚙ computed** below.                                                                             |
| **Search**                                | Name/SKU search must be **Cyrillic↔Latin transparent** (see complexity notes).                                                                                                                                                                                                                  |

### Locked product decisions baked into this contract
1. **`organizationName`** is the API field for the business name (internal entity stays `Tenant`/`TenantId`).
2. **Seeded ordinary defaults** at tenant provisioning: one Partner, one Wallet (Cash), one Warehouse, one Category — fully normal rows (editable, archivable, deletable-when-unreferenced). **No `isSystem` flag, no system partner.** Transaction `partnerId` stays **required (non-null)**.
3. **Order warehouse** = `warehouseId` **nullable**, captured at creation as a non-binding *intended* warehouse, editable while open, **no stock reservation**. Authoritative warehouse + hard stock check happen at **deliver**.
4. **Per-line discount** persisted as `discount` (raw value) + `discountType` (`Percentage`/`Fixed`, frontend uses `"pct"`/`"fixed"`). `Fixed` = currency off the **whole line**, not per-unit. **No transaction-level discount field** — the displayed total discount is the computed sum of line discounts.
5. **Payroll** allows any number of payments per employee+month; payments are **immutable** (canon → backend should drop payroll PUT/DELETE, see §11 note).
6. **Soft-archive** (never hard-delete) for Product, Partner, Wallet, Warehouse; archived rows still count in totals (rule 31).

---

## 1. Auth & Tenant — **REAL** (reset = **MOCKED**)

`organizationName` ↔ rename backend's current `tenantName` to `organizationName` (decision 4.1).

```ts
// Requests
LoginRequest         { phoneNumber: string; password: string }
RegisterRequest      { firstName; lastName; phoneNumber; password; confirmPassword;
                       organizationName: string; email?: string|null; telegramAccount?: string|null }
VerifyPhoneRequest   { phoneNumber: string; code: string }   // code = 4 digits
ForgotPasswordRequest    { phoneNumber: string }
VerifyResetCodeRequest   { phoneNumber: string; code: string } // 4 digits
ResetPasswordRequest     { phoneNumber; code; newPassword; confirmPassword }

// Responses
LoginResponse        { accessToken: string; refreshToken: string }
RegisterResponse     { message: string; expiresInMinutes: number }
VerifyOtpResponse    { success: true;  accessToken; refreshToken; message? }
                   | { success: false; accessToken?: null; refreshToken?: null; message? }
RefreshTokenResponse { accessToken: string; refreshToken: string }
ForgotPasswordResponse   { message: string; expiresInMinutes: number }
VerifyResetCodeResponse  { success: boolean; message? }
ResetPasswordResponse    { success: boolean; message? }
```

| Method | Path                          | Status  | Req → Resp                                       | Notes                                                    |
| ------ | ----------------------------- | ------- | ------------------------------------------------ | -------------------------------------------------------- |
| POST   | `/api/auth/login`             | 200     | LoginRequest → LoginResponse                     | **REAL**                                                 |
| POST   | `/api/auth/register`          | 200     | RegisterRequest → RegisterResponse               | **REAL**. Sends OTP; `expiresInMinutes` = OTP TTL        |
| POST   | `/api/auth/verification`      | 200     | VerifyPhoneRequest → VerifyOtpResponse           | **REAL**. 4-digit code                                   |
| POST   | `/api/auth/refresh-token`     | 200     | `{}` → RefreshTokenResponse                      | **REAL**. Cookie-based                                   |
| POST   | `/api/auth/logout`            | 200     | — → void                                         | **REAL**                                                 |
| POST   | `/api/auth/forgot-password`   | 200     | ForgotPasswordRequest → ForgotPasswordResponse   | **MOCKED → build**. 400 if phone missing                 |
| POST   | `/api/auth/verify-reset-code` | 200     | VerifyResetCodeRequest → VerifyResetCodeResponse | **MOCKED → build**. Mock demo code `1234`                |
| POST   | `/api/auth/reset-password`    | 200/400 | ResetPasswordRequest → ResetPasswordResponse     | **MOCKED → build**. 400 if password <8 chars or mismatch |

> **Two-phase register** (frontend behavior to support): `verification` returns tokens but the app does **not** enter until the user clicks «Начать работу» on the welcome screen. The backend just needs `verification` to return valid tokens on success.

---

## 2. Settings — **MOCKED** (no backend exists)

```ts
Organization { name; address; phone; email; logoUrl: string|null }   // logoUrl: hosted URL (mock used data URL)
ContactType  = "email" | "phone"
TenantUser   { id; name; contact; contactType: ContactType; active; self; online;
               lastActiveAt: string|null }   // lastActiveAt = deactivation date when inactive
InviteUserRequest { method: ContactType; value: string }
```

| Method | Path                                  | Status  | Req → Resp                     | Notes                                                    |
| ------ | ------------------------------------- | ------- | ------------------------------ | -------------------------------------------------------- |
| GET    | `/api/settings/organization`          | 200     | — → Organization               | Business profile = tenant profile                        |
| PUT    | `/api/settings/organization`          | 200     | Organization → Organization    | Logo upload                                              |
| GET    | `/api/settings/users`                 | 200     | — → TenantUser[]               | Includes deactivated                                     |
| POST   | `/api/settings/users/invite`          | 201     | InviteUserRequest → TenantUser | No roles in v1                                           |
| POST   | `/api/settings/users/{id}/deactivate` | 200/404 | — → TenantUser                 | **Never hard-delete** (rule 41). Guard self-deactivation |
| POST   | `/api/settings/users/{id}/reactivate` | 200/404 | — → TenantUser                 | Flips `active`                                           |

---

## 3. Categories — **STALE** (needs `productCount` + 409 gate)

```ts
Category              { id; name; description?: string|null; productCount: number }  // ⚙ productCount
CreateCategoryRequest { name; description?: string|null }
UpdateCategoryRequest = CreateCategoryRequest & { id }
```

| Method | Path                   | Status          | Notes                                                |
| ------ | ---------------------- | --------------- | ---------------------------------------------------- |
| GET    | `/api/categories`      | 200             | full list                                            |
| GET    | `/api/categories/{id}` | 200/404         |                                                      |
| POST   | `/api/categories`      | 201/400         | name ≤100, description ≤500, **duplicate name 400**  |
| PUT    | `/api/categories/{id}` | 200/400/404     | same validation                                      |
| DELETE | `/api/categories/{id}` | 204/404/**409** | **409 if `productCount > 0`** ("содержит N товаров") |

---

## 4. Products — **STALE** (per-warehouse inventory, WAC, archive, images all new)

```ts
Measurement = "Unit"|"Gram"|"Kilogram"|"Liter"|"None"
ProductType = "All"|"Sale"|"Supply"
ProductImage     { id; name; originalUrl; thumbnailUrl? }
ProductPackaging { size: number; label: string|null; barcode: string|null }
ProductInventoryItem { inventoryId; inventoryName; quantity; averageCost }   // ⚙ per-warehouse WAC

Product {
  id; categoryId: number|null; categoryName: string|null;
  name; sku; description?; barcode?;
  salePrice; supplyPrice; retailPrice;            // retailPrice dormant
  measurement: Measurement; type: ProductType;
  lowStockThreshold?: number|null; isLowStock;    // ⚙ isLowStock
  isArchived; packaging?: ProductPackaging; images: ProductImage[];
  inventoryItems: ProductInventoryItem[];          // ⚙
  totalStock: number;                              // ⚙ sum across warehouses
  averageCost: number|null;                        // ⚙ value-weighted WAC; null when no stock
}
CreateProductRequest { categoryId: number|null; name; sku; description?; barcode?;
  salePrice; supplyPrice; retailPrice; measurement; type;
  lowStockThreshold?: number|null; packaging?; attachments?: File[] }
UpdateProductRequest = CreateProductRequest & { id; imagesToDelete?: number[] }

ProductTransaction { id; productId; transactionType: TransactionType; partnerId; partnerName;
  date; quantity /*signed*/; unitPrice; discount }
MovementKind       = "Opening"|"Supply"|"Sale"|"SaleRefund"|"SupplyRefund"|"Adjustment"|"Transfer"
ProductMovement    { id; productId; date; kind: MovementKind; inventoryId; inventoryName;
  quantity /*signed delta*/; balanceAfter /*⚙ running total across warehouses*/ }
```

| Method | Path                              | Status      | Notes                                                                                                   |
| ------ | --------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------- |
| GET    | `/api/products`                   | 200         | full list incl. archived                                                                                |
| GET    | `/api/products/{id}`              | 200/404     |                                                                                                         |
| GET    | `/api/products/{id}/transactions` | 200/404     | newest-first; signed qty                                                                                |
| GET    | `/api/products/{id}/movements`    | 200/404     | newest-first; ⚙ balanceAfter                                                                            |
| POST   | `/api/products`                   | 201/400     | **multipart/form-data** + `attachments`. name 2–250, sku req/≤100/**unique**. **Created at zero stock** |
| PUT    | `/api/products/{id}`              | 200/400/404 | **multipart/form-data** + `attachments`, `imagesToDelete`                                               |
| POST   | `/api/products/{id}/archive`      | 200/404     | soft-delete (rule 32)                                                                                   |
| POST   | `/api/products/{id}/restore`      | 200/404     |                                                                                                         |

> **Backend cleanup:** remove the writable `Product.QuantityInStock` — `InventoryItem` is the sole stock source (rule 17).

---

## 5. Warehouses — **STALE** (live resource is `/api/inventories`; build fresh `/api/warehouses`)

```ts
Warehouse { id; name; location: string|null;
  productCount; totalUnits; stockValue;   // ⚙ all
  isArchived }
WarehouseStockItem { productId; productName; sku; categoryName: string|null;
  measurement; quantity; averageCost /*⚙ warehouse-local WAC*/; value /*⚙*/ }
WarehouseMovement { id; date; kind: MovementKind /*§4 — sourced from the event, not the stock direction*/; productId; productName; measurement;
  counterparty: string|null; note: string|null; quantity /*signed*/;
  balanceAfter /*⚙ per-product per-warehouse running balance*/ }
CreateWarehouseRequest { name; location: string|null }
UpdateWarehouseRequest = CreateWarehouseRequest & { id }
OpeningStockLine    { productId; quantity; unitCost }
AddOpeningStockRequest { items: OpeningStockLine[]; note: string|null }
```

| Method | Path                                 | Status      | Notes                                                                         |
| ------ | ------------------------------------ | ----------- | ----------------------------------------------------------------------------- |
| GET    | `/api/warehouses`                    | 200         | incl. archived; archived-with-stock still reports totals (rule 31)            |
| GET    | `/api/warehouses/{id}`               | 200/404     |                                                                               |
| GET    | `/api/warehouses/{id}/stock`         | 200/404     | products on hand; ⚙ WAC + value                                               |
| GET    | `/api/warehouses/{id}/movements`     | 200/404     | newest-first; ⚙ balanceAfter                                                  |
| POST   | `/api/warehouses`                    | 201/400     | name 2–250, **duplicate name 400**, location ≤250. Created empty              |
| PUT    | `/api/warehouses/{id}`               | 200/400/404 |                                                                               |
| POST   | `/api/warehouses/{id}/opening-stock` | 200/400/404 | per-line: productId req, qty>0, unitCost>0. **Audited stock-in, updates WAC** |
| POST   | `/api/warehouses/{id}/archive`       | 200/404     | soft-delete                                                                   |
| POST   | `/api/warehouses/{id}/restore`       | 200/404     |                                                                               |

---

## 6. Stock Adjustments — **MOCKED** (no backend)

```ts
AdjustmentDirection = "Decrease"|"Increase"
DecreaseReasons = "Damage"|"Expiry"|"Theft"|"RecountDown"|"Other"
IncreaseReasons = "Found"|"RecountUp"|"Other"
StockAdjustment { id; date; warehouseId; warehouseName; productId; productName; sku;
  categoryName: string|null; measurement; direction; quantity /*positive magnitude*/;
  reason; note: string|null; createdBy; balanceAfter /*⚙*/ }
CreateStockAdjustmentRequest { warehouseId; productId; direction; quantity; reason; note: string|null }
```

| Method | Path                     | Status  | Notes                                                                                                  |
| ------ | ------------------------ | ------- | ------------------------------------------------------------------------------------------------------ |
| GET    | `/api/stock-adjustments` | 200     | immutable, newest-first                                                                                |
| POST   | `/api/stock-adjustments` | 201/400 | reason must match direction's set; **Decrease hard-blocked over stock** (rule 20); records cost at WAC |

---

## 7. Transfers — **MOCKED** at target (live `/api/transfers` DTO is stale: missing author/unit, carries unused status)

```ts
TransferLine { productId; productName; sku; measurement; quantity }
Transfer { id; date; fromWarehouseId; fromWarehouseName; toWarehouseId; toWarehouseName;
  note: string|null; createdBy; lines: TransferLine[] }
CreateTransferLine    { productId; quantity }
CreateTransferRequest { fromWarehouseId; toWarehouseId; note: string|null; lines: CreateTransferLine[] }
```

| Method | Path             | Status  | Notes                                                                                                |
| ------ | ---------------- | ------- | ---------------------------------------------------------------------------------------------------- |
| GET    | `/api/transfers` | 200     | immutable, newest-first                                                                              |
| POST   | `/api/transfers` | 201/400 | from≠to, ≥1 line, qty>0; **each line hard-blocked over source stock**; **atomic on both warehouses** |

---

## 8. Partners — **STALE** (balance, opening-balance event, archive, ledger all new)

```ts
PartnerType        = "Customer"|"Supplier"|"Both"
PartnerLedgerStatus = "paid"|"partial"|"unpaid"|"done"
PartnerLedgerEventType = "opening"|"sale"|"supply"|"refund-sale"|"refund-supply"|"payment"|"deposit"|"withdraw"

Partner { id; type; name; phoneNumbers: string[]; address?; email?; telegram?; companyName?;
  balance: number;        // ⚙ net UZS: + partner owes us, − we owe
  openingBalance: number; // immutable event amount, set once at creation
  openingDate: string;
  isArchived; isDeletable /*⚙*/; activityCount /*⚙*/;
  balanceDto?: PartnerBalance|null /*legacy, unused by redesign*/ }
PartnerLedgerEntry { id; type: PartnerLedgerEventType; date; delta /*signed*/; balance /*⚙ running*/;
  reference?; method?; allocation?; itemCount?; status?: PartnerLedgerStatus }
CreatePartnerRequest { type; name; companyName?; address?; email?; telegram?;
  phoneNumbers: string[]; openingBalance: number }
UpdatePartnerRequest { id; type; name; companyName?; address?; email?; telegram?; phoneNumbers: string[] }
// ↑ note: no openingBalance — it is a locked audit event
```

| Method | Path                          | Status          | Notes                                                                       |
| ------ | ----------------------------- | --------------- | --------------------------------------------------------------------------- |
| GET    | `/api/partners`               | 200             | incl. archived                                                              |
| GET    | `/api/partners/{id}`          | 200/404         |                                                                             |
| GET    | `/api/partners/{id}/ledger`   | 200/404         | **NEW**. newest-first; running balance reconciles to `balance`              |
| POST   | `/api/partners`               | 201/400         | name ≥2, type valid, ≥1 phone. **opening balance → immutable ledger event** |
| PUT    | `/api/partners/{id}`          | 200/400/404     | opening balance **not** editable                                            |
| POST   | `/api/partners/{id}/archive`  | 200/404         |                                                                             |
| POST   | `/api/partners/{id}/restore`  | 200/404         |                                                                             |
| DELETE | `/api/partners/{id}`          | 204/404/**409** | **409 when referenced** ("на партнёра ссылаются другие записи")             |
| GET    | `/api/partners/{id}/payments` | 200             | legacy, returns `[]` — superseded by ledger                                 |

---

## 9. Wallets (Касса) — **MOCKED** (whole resource new)

```ts
WalletType = "Cash"|"Card"|"Bank"
Wallet { id; name; type;
  balance: number;       // ⚙ opening + wallet-source components + transfers (rule 15)
  advancesHeld: number;  // ⚙ partner advances physically in this wallet (rule 11)
  ourMoney: number;      // ⚙ balance − advancesHeld (rule 12)
  openingBalance: number; // immutable event
  isArchived; createdBy; createdAt }
WalletOperationKind = "Payment"|"Deposit"|"Expense"|"Withdrawal"|"Transfer"
WalletOperationDirection = "In"|"Out"
WalletOperation { id; date; kind; direction; paymentNumber: string|null; party;
  amount; balanceAfter /*⚙ running*/; transferId: number|null }
WalletTransfer { id; date; fromWalletId; fromWalletName; fromWalletType;
  toWalletId; toWalletName; toWalletType; amount; createdBy; note: string|null }
CreateWalletRequest { name; type; openingBalance: number }
UpdateWalletRequest { id; name }            // type + opening immutable (rule 16)
CreateTransferRequest { fromWalletId; toWalletId; amount; note: string|null }
```

| Method | Path                           | Status      | Notes                                                                                               |
| ------ | ------------------------------ | ----------- | --------------------------------------------------------------------------------------------------- |
| GET    | `/api/wallets`                 | 200         | incl. archived; archived-with-money still reports balance (rule 31)                                 |
| GET    | `/api/wallets/{id}`            | 200/404     |                                                                                                     |
| GET    | `/api/wallets/{id}/operations` | 200/404     | newest-first; ⚙ balanceAfter                                                                        |
| GET    | `/api/wallets/{id}/transfers`  | 200/404     | transfers touching this wallet                                                                      |
| POST   | `/api/wallets`                 | 201/400     | name 2–250 unique, type valid, openingBalance ≥0                                                    |
| PUT    | `/api/wallets/{id}`            | 200/400/404 | name only                                                                                           |
| POST   | `/api/wallets/transfers`       | 201/400/404 | from≠to, amount>0, **hard-blocked over from-balance**; immutable; appends an operation to each side |
| POST   | `/api/wallets/{id}/archive`    | 200/404     |                                                                                                     |
| POST   | `/api/wallets/{id}/restore`    | 200/404     |                                                                                                     |

---

## 10. Payments — **MOCKED** at redesigned contract (live DTO is the removed-enum legacy model)

> The frontend carries **two** payment models. Build the **redesigned** one below. The **legacy** model (`PaymentMethod`/`currency`/`exchangeRate`, allocation types `Sale|Supply|...`) is transitional — still referenced by Payroll and the New-Sale inline debt-payment flow — see complexity notes §"Two payment models".

```ts
PaymentType          = "Transaction"|"Deposit"|"Withdrawal"|"Payroll"|"General"
PaymentDirection     = "Income"|"Expense"
PaymentSourceType    = "Wallet"|"Advance"
PaymentAllocationKind = "TransactionSettlement"|"AdvanceCredit"|"ChangeReturn"

PaymentSource { id; sourceType; walletId: number|null; walletName: string|null;
  walletType: WalletType|null; amount }
PaymentAllocationEntry { id; allocationType: PaymentAllocationKind;
  transactionId: number|null; reference: string; amount }

PaymentRecord {
  id; number /*«P-520»*/; date; type; direction;
  partnerId: number|null; partnerName: string|null; partnerType: PartnerType|null;
  employeeId: number|null; employeeName: string|null; employeePosition: string|null;
  walletId; walletName; walletType;     // primary wallet the payment moves through
  amount; allocationSummary /*⚙*/;
  description: string|null;             // General
  period: string|null; salary: number|null;  // Payroll
  createdBy; sources: PaymentSource[]; allocations: PaymentAllocationEntry[] }

OutstandingTransaction { id; date; type: "Sale"|"Supply"; total; paid; remaining /*⚙*/ }
PaymentFormData {
  partners: { id; name; type; balance /*⚙*/; advance /*⚙*/ }[];
  employees:{ id; name; position; salary }[];
  wallets:  { id; name; type; balance /*⚙*/ }[] }
SettlementInput { transactionId; amount }
CreatePaymentRecordRequest {
  type; direction; partnerId: number|null; employeeId: number|null;
  walletId; amount; description: string|null; period: string|null;
  settlements: SettlementInput[] }
```

| Method | Path                                       | Status  | Notes                                                                                                                                                |
| ------ | ------------------------------------------ | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| GET    | `/api/payments`                            | 200     | newest-first                                                                                                                                         |
| GET    | `/api/payments/form-data`                  | 200     | reference data for create modal                                                                                                                      |
| GET    | `/api/payments/outstanding?partnerId={id}` | 200/400 | FIFO oldest-first; **400 if partnerId missing**                                                                                                      |
| GET    | `/api/payments/{id}`                       | 200/404 |                                                                                                                                                      |
| POST   | `/api/payments`                            | 201/400 | partnerId req for Transaction/Deposit/Withdrawal; employeeId req for Payroll; description req for General; amount>0. **Excess settlement → advance** |

---

## 11. Transactions — Sales / Supplies / Refunds — **STALE / partial** (read + refund + JSON-create are the target)

```ts
TransactionType   = "Sale"|"Supply"|"SaleRefund"|"SupplyRefund"
TransactionStatus = "Open"|"Closed"|"PartiallyPaid"|"Overdue"
PaymentStatus     = "paid"|"partial"|"unpaid"
DiscountType      = "pct"|"fixed"

TransactionRecord {
  id; partnerId; partnerName; notes?; date; transactionNumber?;
  totalDue; totalPaid; remaining /*⚙ = due−paid*/;
  type; status; lines: TransactionLine[];
  time?; warehouseName?; createdBy?; paymentStatus? /*⚙*/;
  originalTransactionId?; originalTransactionNumber?; refundReason?;  // refunds
  payments?: TransactionPaymentLine[]; attachments?: TransactionAttachment[] }
TransactionLine { id; productId; productName; transactionId; unitPrice; quantity;
  total /*⚙ net after discount*/; discount; unit?; discountType?: DiscountType }
TransactionPaymentLine { id; date; method; amount }
TransactionAttachment  { name; kind: "pdf"|"img"; size }

CreateTransactionEntryRequest {
  direction: "Sale"|"Supply"; partnerId; warehouseId;
  lines: { productId; quantity; unitPrice; discount; discountType }[];
  notes?; walletId; paidAmount; settlements: SettlementInput[];
  overpayment: "change"|"advance"; attachments? }
CreateRefundRequest {
  reason: string;
  lines: { productId; productName; quantity; unitPrice }[] }
```

| Method | Path                            | Status      | Notes                                                                                                                                                                                                                         |
| ------ | ------------------------------- | ----------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GET    | `/api/transactions`             | 200         | sales + supplies + refunds, newest-first                                                                                                                                                                                      |
| GET    | `/api/transactions/{id}`        | 200/404     | enriched with payments/attachments/refund linkage                                                                                                                                                                             |
| POST   | `/api/transactions`             | 201/400     | **`application/json`** → POS create (New Sale/Supply). Validates partner, warehouse, ≥1 line qty>0. **Decrements stock at WAC + settles payment** (see complexity notes). Other content-types pass through (legacy multipart) |
| POST   | `/api/transactions/{id}/refund` | 201/400/404 | type-match (no refund-of-refund), mandatory reason, **per-line cumulative cap** ≤ original qty                                                                                                                                |

---

## 12. Orders (Заказы) — **STALE** (status history, warehouse, sale link, delivery date/time, per-line SKU all new)

```ts
OrderStatus = "Pending"|"Processing"|"Shipping"|"Delivered"|"Cancelled"|"Rejected"|"Returned"
OrderSource = "None"|"Telegram"|"OmborWeb"
DiscountType = "pct"|"fixed"

Order {
  id; orderNumber; customerId; customerName; customerType; customerBalance /*⚙*/;
  date; status; source; deliveryAddress: string|null;
  deliveryDate: string|null; deliveryTime: string|null; notes: string|null;
  warehouseId: number|null; warehouseName: string|null;    // intended warehouse
  saleId: number|null;       // ⚙ set on Delivered
  total /*⚙*/; lines: OrderLine[]; history: OrderStatusEvent[] }
OrderLine { id; productId; productName; sku /*⚙*/; measurement /*⚙*/;
  quantity; unitPrice; discount; discountType; total /*⚙*/ }
OrderStatusEvent { at; from: OrderStatus|null; to: OrderStatus; by }

CreateOrderRequest { customerId; source; warehouseId; deliveryAddress?; deliveryDate?;
  deliveryTime?; notes?; lines: { productId; quantity; unitPrice; discount; discountType }[] }
UpdateOrderRequest = CreateOrderRequest-shape & { id; warehouseId?: number|null }
DeliverOrderRequest { id; warehouseId }
```

| Method | Path                            | Status      | Transition / Notes                                                                             |
| ------ | ------------------------------- | ----------- | ---------------------------------------------------------------------------------------------- |
| GET    | `/api/orders?searchTerm&status` | 200         | newest-first                                                                                   |
| GET    | `/api/orders/{id}`              | 200/404     | full detail + history                                                                          |
| POST   | `/api/orders`                   | 201/400     | customer + warehouse + ≥1 line. Creates **Pending**; **no stock reserved**                     |
| PUT    | `/api/orders/{id}`              | 200/400/404 | edit while open. `warehouseId`: **undefined=keep, null=clear, value=set**                      |
| POST   | `/api/orders/{id}/process`      | 200/404     | Pending → Processing                                                                           |
| POST   | `/api/orders/{id}/ship`         | 200/404     | Processing → Shipping                                                                          |
| POST   | `/api/orders/{id}/deliver`      | 200/400/404 | Shipping → Delivered. **Requires warehouseId + hard stock check**; promotes to Sale (`saleId`) |
| POST   | `/api/orders/{id}/cancel`       | 200/404     | pre-delivery → Cancelled                                                                       |
| POST   | `/api/orders/{id}/reject`       | 200/404     | pre-delivery → Rejected                                                                        |
| POST   | `/api/orders/{id}/return`       | 200/404     | Delivered → Returned                                                                           |

---

## 13. Templates (Шаблоны) — **STALE** (lastUsedAt + per-item SKU/unit new)

```ts
TemplateType = "Supply"|"Sale"
Template { id; name; partnerName /*⚙*/; partnerId; total /*⚙*/; type;
  lastUsedAt: string|null /*⚙*/; items: TemplateItem[] }
TemplateItem { id; productName; productId; sku /*⚙*/; measurement /*⚙*/;
  quantity; unitPrice; discount }   // ⚠ percentage-only today — see note
CreateTemplateRequest { name; partnerId; type; items: { productId; quantity; unitPrice; discount? }[] }
UpdateTemplateRequest { id; name; partnerId; type; items: { id; productId; quantity; unitPrice; discount? }[] }
```

| Method | Path                             | Status      | Notes                                               |
| ------ | -------------------------------- | ----------- | --------------------------------------------------- |
| GET    | `/api/templates?searchTerm&type` | 200         | editable basket; not immutable                      |
| GET    | `/api/templates/{id}`            | 200/404     |                                                     |
| POST   | `/api/templates`                 | 201/400     | name, partnerId, type, ≥1 item (qty>0, unitPrice>0) |
| PUT    | `/api/templates/{id}`            | 200/400/404 | full replace                                        |
| DELETE | `/api/templates/{id}`            | 200/404     | **no reference gate** (just a basket)               |

> ⚠ **Discount gap to resolve:** template items currently carry only `discount?: number` (treated as **percentage**), with no `discountType`. To honor the per-line discount decision (§0.4), add `discountType` to template items so a fixed-currency discount survives a template→transaction load. Flagged for alignment.

---

## 14. Employees & Payroll — **REAL** (but payroll needs canon alignment)

```ts
EmployeeStatus = "Active"|"Terminated"|"OnVacation"
ContactInfo { phoneNumbers: string[]; email?; address?; telegramAccount? }
Employee { id; name; position; status; salary; dateOfEmployment; contactInfo? }
CreateEmployeeRequest { name; position; salary; status; dateOfEmployment; contactInfo? }
UpdateEmployeeRequest = CreateEmployeeRequest & { id }
```

| Method         | Path                                       | Status | Notes                                                                            |
| -------------- | ------------------------------------------ | ------ | -------------------------------------------------------------------------------- |
| GET            | `/api/employees?searchTerm`                | 200    | **REAL**                                                                         |
| GET            | `/api/employees/{id}`                      | 200    | **REAL**                                                                         |
| POST           | `/api/employees`                           | 200    | **REAL**                                                                         |
| PUT            | `/api/employees/{id}`                      | 200    | **REAL**                                                                         |
| DELETE         | `/api/employees/{id}`                      | 200    | **REAL** — but UI never calls it; **termination is a status change**, not delete |
| GET            | `/api/employees/{id}/payrolls`             | 200    | **REAL**; returns `Payment[]`                                                    |
| POST           | `/api/employees/{id}/payrolls`             | 200    | **REAL**; CreatePayrollRequest                                                   |
| GET/PUT/DELETE | `/api/employees/{id}/payrolls/{paymentId}` | 200    | **REAL** today                                                                   |

> **Canon alignment (decision 4.7 + business-rules rule 1):** payroll payments are **immutable** and allowed **any number per employee+month**. The backend should **remove payroll PUT/DELETE** (corrections via reverse payment), drop any one-per-month constraint, and converge payroll onto the redesigned Payment model (UZS-only, no `currency`/`exchangeRate`/`method` enum). The legacy `CreatePayrollRequest` (currency/method/exchangeRate) is transitional.

---

## 15. Debts (Долги) — **MOCKED** (no endpoint; served read model)

```ts
DebtDirection = "Receivable"|"Payable"
Debt {
  transactionId; number; direction; transactionType: TransactionType;
  partnerId; partnerName; partnerCompany: string|null; partnerType;
  date; dueDate;
  total; paid; remaining /*⚙*/; ageDays /*⚙*/; overdueDays /*⚙ >0 = overdue*/ }
```

| Method | Path         | Status | Notes                                                                                               |
| ------ | ------------ | ------ | --------------------------------------------------------------------------------------------------- |
| GET    | `/api/debts` | 200    | full set of unpaid/partially-paid transactions; **derived from transactions** — not a stored entity |

---

## 16. Dashboard (Главное) — **MOCKED** (no endpoint; aggregated read model)

```ts
DashboardPeriod = "today"|"week"|"month"
DashboardKpi { value; deltaPct: number|null; count; trend: number[] }
DashboardData {
  businessName; period;
  revenue: DashboardKpi;
  receivable: DashboardKpi; payable: DashboardKpi;
  overdue: DashboardKpi & { partnerCount: number };   // aged 31+ days
  series: { label; sales; supplies; payin; payout; walletPayin: number[]; walletPayout: number[] }[];
  wallets: { id; name; type: "cash"|"bank" }[];
  aging: { bucket: "0-7"|"8-30"|"31-60"|"60+"; amount }[];
  topDebtors: { partnerId; name; company: string|null; amount }[];
  recentTransactions: { id; date; partnerName; type: "Sale"|"Supply"; total; paid; status }[] }
```

| Method | Path                                       | Status | Notes                                                                                                                                                                                         |
| ------ | ------------------------------------------ | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GET    | `/api/dashboard?period=today\|week\|month` | 200    | default `month`. **Debt KPIs must reconcile with `/api/debts`** (same derivation). `overdue` = receivables aged **31+ days** (distinct from `/debts` due-date overdue — see complexity notes) |

---

## Build-order suggestion

Foundations → dependents: **multi-tenancy + seeded defaults → Products/Warehouses/InventoryItem + WAC → Partners (balance+ledger) → Wallets → Payments (source/allocation) → Transactions (create with stock+settlement, refund) → Stock Adjustments + Transfers → Orders (+ promotion) → Debts + Dashboard read models → Settings/users → archive + audit log as cross-cutting interceptors.**
