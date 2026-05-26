# Ombor — MVP plan

**Status:** scope-locked, pending execution
**Last updated:** 2026-04-20

This document defines what ships in v1. Anything not listed here is deferred. For product vision, target users, and the reasoning behind these decisions, see the project context document.

---

## Feature scope

### 1. Products

Every product has a name, SKU, optional description, optional barcode, optional category, unit of measurement, and packaging details (package size, label, barcode) where applicable. Products carry three prices — sale, supply, and retail — interpreted according to the product's type (Sale, Supply, or Both). Products can have multiple images.

Products also carry a weighted-average cost that updates atomically whenever stock is received. This is the basis for real profit reporting.

Products can be archived but not hard-deleted once referenced by any transaction or order.

**Acceptance:** a user can create a product with all fields above, edit it, archive it, and search for it by name or SKU. Weighted-average cost recomputes correctly when stock is received at a different price than previous stock.

---

### 2. Partners

A partner is a customer, a supplier, or both. Partners carry contact info (phone numbers, email, address, telegram), an optional company name, and a balance. Balance is computed per read from the event log (transactions, payments, allocations), not stored directly — this prevents drift.

Opening balance is entered at partner creation and recorded as an auditable ledger event with who entered it and when, so the starting point of the balance can always be explained.

Partners can be archived but not hard-deleted once referenced.

**Acceptance:** a user can create a partner with opening balance, view current balance and a chronological event log explaining every change, and archive the partner.

---

### 3. Transactions

Five transaction types:
- **Sale** — goods leave the business, partner owes
- **Supply** — goods enter the business, business owes partner
- **SaleRefund** — partial or full reversal of a sale
- **SupplyRefund** — partial or full reversal of a supply
- **WriteOff** — stock leaves without a partner (damage, theft, expiry, loss)

Each transaction has lines (product, quantity, unit price, discount), a partner (except WriteOff), a warehouse (which inventory is affected), an optional note, optional attachments (photos of paper receipts etc.), and a date. Transactions are immutable once saved — corrections are made via a reverse-type transaction, never by editing.

Discounts work per line (percentage or fixed amount) and per whole transaction. Transaction UI shows the partner's current balance at the top so the user sees credit state while entering.

At transaction creation, the user can optionally enter payments inline (one or many). Payments recorded this way are allocated to the transaction via the settlement modal if the user chooses, or kept as partner advance otherwise.

Negative stock is hard-blocked at MVP — the system refuses a sale if stock is insufficient.

**Acceptance:** a user can create each transaction type, attach files, add lines with both discount types, see the partner balance during entry, and cannot edit or delete after save. Reverse transactions correctly adjust partner balance and inventory.

---

### 4. Payments

Payments are partner-level events, not owned by transactions. A payment has components (currency, method, amount, exchange rate — multiple components allowed so a partner can pay part in cash, part by card) and allocations (optional references to which transactions the payment settles).

Five payment types:
- **Transaction** — payment toward one or more outstanding transactions
- **Deposit** — partner sends money to business outside any transaction, held as advance
- **Withdrawal** — business returns money to partner outside any transaction
- **Payroll** — payment to an employee
- **General** — catch-all for payments that don't fit above

Three allocation types within a payment: advance payment (unallocated credit), transaction settlement, or change return.

Payments are immutable. Corrections are via reverse payment, never edit or delete. Payroll payments follow the same rule — no PUT/DELETE on payroll.

Payments can be in UZS or USD. The exchange rate used is stored on the payment event itself, so historical records reflect the rate as of that date.

The settlement UI presents outstanding transactions, lets the user auto-allocate in chronological order (one button), or manually distribute the amount across specific transactions. Any leftover is kept as advance.

**Acceptance:** a user can create each payment type, split a payment across multiple components, allocate across transactions manually or via FIFO, see advance balance separately from debt balance, and cannot edit or delete payments.

---

### 5. Inventory

A business can have 1-3 warehouses. Each warehouse holds inventory items (product + quantity + weighted-average cost). Inventory items are created via a stock-in event, which is an auditable ledger event recording who added the stock, when, at what cost, and why (opening stock, supply transaction, transfer receipt, reversal).

Inter-warehouse transfers move stock between warehouses as a tracked operation. The sending warehouse's quantity decreases and the receiving warehouse's quantity increases, with a transfer record tying the two movements together.

Every stock movement (sale, supply, refund, write-off, transfer) produces a ledger event visible in the inventory history.

Current stock per product is the sum of InventoryItem quantities across warehouses. The Product entity does not carry an independent quantity field — InventoryItem is the single source of truth.

**Acceptance:** a user can create warehouses, enter opening stock, view stock per warehouse and total across warehouses, transfer stock between warehouses, and see a movement history explaining every quantity change.

---

### 6. Orders

An order is a pending transaction — a customer has asked to buy goods, but nothing has been delivered or paid yet. Orders have lines (product, quantity, unit price, discount), a customer, an optional delivery address, and an optional source (e.g., telegram bot, web — full telegram integration is deferred, but the source field is present).

Orders progress through states (Pending → Processing → Shipping → Delivered; or Cancelled / Rejected / Returned from appropriate states). A delivered order promotes to a Sale transaction, reserving the sale event in transaction history.

**Acceptance:** a user can create an order, move it through states, and promote it to a transaction. The state machine prevents invalid transitions.

---

### 7. Templates

Reusable baskets of products tied to a specific partner. If a reseller sells the same 15 items every week to the same small shop, a template loads those 15 items with quantities and prices into a new transaction in one click.

Templates are typed (Sale or Supply) and tied to a partner.

**Acceptance:** a user can create a template from an existing transaction or from scratch, and load a template into a new transaction.

---

### 8. Employees and payroll

Employees are staff of the business (not system users — system login is single-user per business in MVP). Each employee has a name, position, salary, employment status (Active, OnVacation, Terminated), date of employment, and contact info.

Payroll is a specialized payment type. Employees can receive payments mid-month, advances for future months, or regular salary payouts — all tracked as Payroll-type payments linked to the employee.

**Acceptance:** a user can add employees, record payroll payments, and see each employee's payment history.

---

### 9. Narrow audit

Events affecting money or stock are immutable and carry who/when metadata. This applies to:

- Transactions (all types including WriteOff)
- Payments (all types including Payroll)
- Stock-in events, including opening stock
- Inter-warehouse transfers
- Opening balance on partner creation
- Payment allocations

Corrections to any of the above are done via reverse events, never by editing. Non-financial edits (product name, partner phone, etc.) are not audited in MVP.

This is a targeted audit scope, not a general audit framework. A general framework can be added in v2 if needed.

**Acceptance:** every financial/stock event shows who created it and when. No endpoint exists to edit or hard-delete these events.

---

### 10. Multi-currency capture

The system operates natively in UZS. USD is supported as a capture currency — when a partner pays in USD, the user enters the USD amount and the exchange rate for that date. The system stores the UZS equivalent as the canonical value.

Exchange rate is auto-fetched daily from CBU but overridable by the user per event. The rate used on a specific event is stored on that event, so historical records reflect the rate as of that date.

This is capture, not true multi-currency. A partner's balance is stored in UZS. For customers with genuine foreign-currency-denominated debts (e.g., an exporter with a USD contract), this model will cause phantom balance drift as rates move — noted, deferred to v2.

**Acceptance:** a user can enter a payment in USD with a rate, see the UZS equivalent stored, and view the rate used on every historical event.

---

### 11. Multi-tenancy

Every tenant-scoped entity (Product, ProductImage, Partner, Category, Inventory, InventoryItem, TransactionRecord, TransactionLine, Payment, PaymentComponent, PaymentAllocation, PaymentAttachment, Template, TemplateItem, Employee, Order, OrderLine) is filtered by TenantId at the query layer. Users cannot see or affect data belonging to other tenants.

**Acceptance:** with two test organizations and two test users, each user sees only their organization's data across every endpoint.

---

### 12. Localization

UI available in UZ-Latin, UZ-Cyrillic, and RU. Users select their language in settings; the UI switches completely.

Search is Cyrillic↔Latin interoperable for product and partner names — typing "shokolad" matches "шоколад" and vice versa. Implemented at the query layer.

**Acceptance:** switching languages in settings updates the entire UI. Product and partner search returns results regardless of whether the query and stored name are in Cyrillic or Latin.

---

### 13. Dashboards

Core reports visible to the user:

- Total receivables (money partners owe)
- Total payables (money business owes partners)
- Inventory value at weighted-average cost
- Sales over time (configurable range)
- Top products by sales volume and revenue
- Profit — revenue minus weighted-average COGS for the period

**Acceptance:** each report reflects current data and updates after new transactions.

---

### 14. Mobile (read-only companion)

Mobile app is a read-only companion for v1:

- View partners and their balances and event log
- View transactions
- View payments
- View products
- View dashboards

No write flows on mobile in v1. Barcode scanning is deferred to v2 along with mobile write flows.

**Acceptance:** a user logged in on mobile can browse all read-only surfaces listed above, reflecting real data from the server.

---

### 15. Attachments

Transactions and payments can have file attachments (photos of paper receipts, signed delivery slips, etc.). Multiple attachments per event. Already implemented; no change needed for MVP scope.

---

## Deferred to v2 or later

Explicitly out of scope for v1. Listed so nobody adds them back without a decision.

- Receipt printing, thermal printer support, cash-drawer integration, fiscal memory
- Production, bills of materials, raw-material-to-finished-goods conversion
- Batch, lot, and expiry tracking
- Product variants (flavor, size, color as modifiers on one SKU)
- Per-partner custom pricing
- Multiple cash registers / money location tracking / cashbox reconciliation
- E-faktura, Soliq integration, Didox integration, 1C export
- Offline mode
- English UI
- True multi-currency (native foreign-currency debts, rate-move revaluation)
- Credit limits per partner
- Aging buckets (30/60/90 day reports)
- Barcode scanning via camera
- Mobile write flows (creating transactions, payments, orders from mobile)
- Multi-user per organization, full role-based permissions
- In-place editing of transactions or payments (always via reverse events in v1)
- Data import/export for onboarding (flagged as critical for launch — must be designed and built before public launch, but scope and approach is a separate discussion)
- Telegram bot integration for order capture (OrderSource field exists, integration does not)
- General-purpose audit framework beyond money/stock events
- Interest accrual, contract/agreement layer above transactions, barter / in-kind payments

---

## Known gaps in current implementation

This is not a technical change list (that's a separate document). These are features from the scope above that are not yet built, noted here so the MVP plan reflects reality.

- Weighted-average cost (field, update logic, profit dashboard)
- WriteOff transaction type
- Inter-warehouse transfers (entity and endpoint)
- Transaction-to-warehouse linkage (which inventory is affected)
- Archive (soft-delete) for Product and Partner — current endpoints are hard-delete
- Reverse mechanism for transactions and payments
- Opening balance and opening stock as auditable events (currently raw number assignments)
- Multi-tenancy enforcement across all tenant-scoped entities
- Cyrillic↔Latin search parity
- Server-side pagination, filtering, and search on list endpoints
- Removal of PUT/DELETE on payroll endpoints
- Removal of Product.QuantityInStock as writable (InventoryItem becomes sole source of truth)

---

## Definition of "MVP done"

All features above are implemented and acceptance criteria pass. Three design-partner businesses are using the product in real operations. Data import or onboarding assistance exists for those three businesses specifically, even if not a fully polished import feature.

No wider launch until those three design partners have used the product for at least four weeks and given structured feedback that's been reviewed.