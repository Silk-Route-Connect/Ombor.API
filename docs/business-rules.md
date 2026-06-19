# Ombor — business rules

**Status:** active decision record — current best thinking, revisable when we learn better; not a frozen spec. Supersedes `rules.md` and the domain-model / enum sections of `claude-context.md`.
**Last updated:** 2026-06-13

The spec the implementation follows: hard rules first, then the domain model they constrain, then the consolidated enum reference. Pair with `mvp-plan.md` for feature scope and `product-brief.md` for vision and reasoning. When a rule and a feature description conflict, the rule wins; when a decision changes, change the rule — don't work around it silently.

---

## Hard rules

These must never be violated. If implementation requires breaking one, stop and raise it.

### A. Immutability & corrections

1. **Transactions, payments, payroll, stock adjustments, and transfers are immutable.** No PUT or DELETE on TransactionRecord, Payment, PaymentComponent, PaymentAllocation, Payroll, StockAdjustment, or Transfer. Corrections are made only via reverse / counter events. (Payroll specifically: no PUT/DELETE on payroll payments; an employee may receive any number of payroll payments per month; payroll flows through the redesigned payment model — no PaymentMethod / currency / exchange-rate fields.)
2. **Refund transactions require `OriginalTransactionId`** (required for SaleRefund and SupplyRefund; null for all other transaction types).
3. **Refund type must match original type.** SaleRefund references a Sale only; SupplyRefund references a Supply only.
4. **A refund cannot be refunded.** `OriginalTransactionId` must point to a non-refund transaction.
5. **Cumulative refunded quantity per original line must not exceed the original line quantity.** Enforced at write time across all refunds against the same original, on both frontend and backend. Remaining-refundable is shown per line.
6. **Multiple refunds against the same original transaction are allowed**, subject to rule 5.
7. **A refund requires a mandatory reason / note.** This distinguishes a real return from a data-entry correction.

### B. Payments — source / allocation model

8. **Every payment is two-sided. The sum of source components must equal the sum of _settling_ allocations — TransactionSettlement + AdvanceCredit.** ChangeReturn is excluded from this balancing identity (it is a memo, not a settling allocation — see rule 10). Reject any payment whose sources and settling allocations don't balance.
9. **PaymentComponent is the source side.** `SourceType ∈ {Wallet, Advance}`. A Wallet source carries `WalletId` + the amount moved through that wallet (net of any change returned — see rules 10 and 15). An Advance source carries an amount drawn from the partner's advance claim and references no wallet. PaymentComponent carries no method, currency, or exchange rate.
10. **PaymentAllocation is the destination side.** `AllocationType ∈ {TransactionSettlement, AdvanceCredit, ChangeReturn}`. **TransactionSettlement and AdvanceCredit are settling allocations** and sit inside the rule-8 identity: a TransactionSettlement references the `TransactionId` it settles (the transaction carries its own type, so allocation type is not split by transaction type), and an AdvanceCredit parks money as a partner claim. **ChangeReturn is not a settling allocation** — it is a stored audit memo recording cash handed straight back to the partner, excluded from the rule-8 identity, from wallet-balance computation (rule 15), and from partner-balance computation. It exists so the user can later see why a wallet — and a partner's balance — didn't move by the full amount tendered.
11. **Advance is a per-partner claim on cash that physically sits in a wallet** — not a separate money location. Settling from an Advance source moves no wallet money; it only consumes the claim.
12. **"Our money" in a wallet = wallet balance − advances held.** Derived per read, never stored. **All derived balances — partner balance, wallet balance, our-money — are computed by the backend and served via the API. Clients never recompute a balance from event lists.**
13. **Payment type is fixed by context in guided flows and is not user-changeable there.** The user specifies payment type only in the standalone (non-guided) payment flow.
14. **Payment direction is auto-derived where unambiguous and user-set where not:**
    - Transaction-type: derived (Sale → Income, Supply → Expense).
    - Payroll: always Expense.
    - Deposit / Withdrawal: derived when the partner is a single type (Customer or Supplier); user-set when the partner is Both.
    - General: always user-set.

### C. Wallets

15. **Wallet balance is computed, not stored** — opening balance + Wallet-type payment components + inter-wallet transfers. Advance-source components never affect a wallet balance. **A Wallet component is recorded net of any change returned**: a customer who tenders 1000 to settle 600 and takes 400 back produces a Wallet component of 600, not 1000 — so a ChangeReturn allocation needs no separate wallet effect.
16. **Opening wallet balance and inter-wallet transfers are auditable, wallet-affecting events** — never raw number assignments.

### D. Inventory & cost

17. **InventoryItem is the sole source of truth for stock.** `Product.QuantityInStock` is removed — not read, not written.
18. **Weighted-average cost is stored on InventoryItem**, updated atomically on every stock-in event (Supply, transfer receipt, SaleRefund, StockAdjustment increase, opening stock). Formula: `((existing_qty × existing_cost) + (incoming_qty × incoming_cost)) / (existing_qty + incoming_qty)`. It is a stored field, not a view (sequential calculation, not aggregable).
19. **Stock always leaves at WAC.** A Sale's stock-out is COGS; a StockAdjustment decrease is loss, reported as a distinct line separate from COGS.
20. **Hard-block negative stock** at write time. A StockAdjustment decrease cannot take stock below zero.
21. **Stock moves in base units.** A line entered in packages decrements base units (package count × package size). The entered package count is retained on the line for audit.
22. **Product creation never moves stock.** A product is always created at zero quantity — it is a definition, nothing more. All initial stock enters through the **warehouse opening-stock flow:** an audited stock-in event with per-product quantity and unit cost, scoped to a warehouse. Products expose stock only as served read models — per-warehouse InventoryItems and the aggregates `totalStock` / value-weighted `averageCost`.

### E. Stock adjustments

23. **Stock leaving for a non-sale reason** (damage, theft, expiry, loss) **or returning to correct a mistaken decrease is a StockAdjustment** — a standalone, immutable, audited, partner-less, payment-less stock event. It is **not** a transaction type.
24. **A StockAdjustment has a direction (Decrease | Increase) and a mandatory reason.** A Decrease records cost = quantity × WAC snapshot at event time and surfaces as a distinct loss line in profit reporting. An Increase is an audited stock-in that restores units at current WAC; it carries no linkage to any prior decrease. (Linkage machinery tying a correcting Increase to the Decrease it reverses is a v2 enhancement.)
25. **StockAdjustment covers loss (Decrease) and correction / found-stock (Increase) only.** Systematic physical-inventory / cycle-count reconciliation is v2.

### F. Audit

26. **Audit logs create / edit / archive / delete across all mutable master data** (Product, Partner, Wallet, Warehouse, Employee, Template, Order) **and every money / stock event** (Transaction, Payment, PaymentComponent, PaymentAllocation, StockAdjustment, Transfer, InventoryItem change, opening balance / opening stock). Capture: actor, timestamp, event type, entity type, entity id, before-values, after-values.
27. **Audit does not make immutable events editable.** Master data is editable and its edits are logged. Transactions, payments, and stock events remain immutable and are corrected via counter-events.
28. **A single audit log backs one Activity Log screen**, filterable by entity, actor, and date.

### G. Archive

29. **Archive (soft-delete via `IsDeleted`) is allowed for Product, Partner, Wallet, and Warehouse only.** No other entities archive in MVP.
30. **Archiving is never blocked by existing references.** Archive is not delete; references continue to resolve to the archived entity.
31. **An archived wallet or warehouse that still holds a balance or stock still counts in totals.**
32. **Never hard-delete an entity with referential history.**

### H. Currency

33. **Single currency (UZS) in MVP.** No per-entity currency, no exchange rate, no rate source. Multi-currency is a dedicated future effort, not a v1 deferral to be half-built now.

### I. Multi-tenancy

34. **Every organization-scoped query — including new endpoints — filters by `OrganizationId` through the shared scoping mechanism**, not a hand-written filter per endpoint, so a new endpoint cannot silently skip it. (The backend entity is `Organization`, keyed by `OrganizationId`; the concrete scoping mechanism is confirmed in the Code audit.) Organization-scoped entities: Product, Partner, Category, Inventory, InventoryItem, Wallet, TransactionRecord, TransactionLine, Payment, PaymentComponent, PaymentAllocation, StockAdjustment, Transfer, Template, TemplateItem, Employee, Order, OrderLine, audit log entries.

### J. Multi-user & scope discipline

35. **Multi-user in MVP with no role-based access** — every user in an organization can perform every action. Audit is the sole accountability mechanism. Roles and permissions are v2; do not design them speculatively.
36. **Do not add features beyond `mvp-plan.md` without an explicit decision.** Surface scope questions; don't expand silently.

### K. Discounts

37. **Discounts are line-level only** (percentage or fixed amount, per line). There is no transaction-level discount input. A **fixed** discount is a currency amount taken off the **whole line** (`unitPrice × quantity − discount`), not a per-unit amount, and is clamped to the line gross. A **percentage** discount is `unitPrice × quantity × discount / 100`. Both `discount` and `discountType` are persisted end to end — a fixed amount is never converted to a percentage (so it doesn't rescale when a mutable line in an Order or Template is later re-priced).
38. **The transaction's total discount is computed** — the sum of line discounts in currency, plus a derived effective percentage. A bulk "apply X% to all lines" control **overwrites** each line's discount; it does not stack and creates no separate total field.

### L. Partners & payment gating

39. **Every sale requires an explicitly chosen partner** — there is no anonymous or system "walk-in" partner, and `Transaction.partnerId` is required (non-null) for all sales, POS included. To support walk-in retail, the user creates and names an ordinary partner (e.g. «Розничный покупатель», «Не сохранённый клиент») and links walk-in sales to it; this is the user's choice, not a system concept. The starter partner seeded at setup (rule 42) is an ordinary partner the user may use, rename, or delete like any other.
40. **Advance gating:** an AdvanceCredit allocation is permitted only when the partner has no remaining outstanding debt after the payment's other settling allocations. The backend rejects it otherwise; the UI offers the advance option only at zero outstanding debt. On overpayment against existing debt, change return is the default and settling other open transactions is the opt-in (UI behavior per design-handoff).

### M. Users

41. **Users are deactivated, never hard-deleted.** A deactivated user cannot authenticate but remains resolvable as an audit actor and in all historical attributions. Reactivation is allowed. (Same principle as rule 32: audit entries reference users; deleting one would orphan the trail.)

### N. Organization setup

42. **Organization setup seeds starter records: one Cash wallet, one warehouse, one category, and one partner.** All four are **ordinary entities** with no special protection or system flag — editable, archivable, and deletable under the normal rules, indistinguishable in the data model from records the user creates later. They exist only so a brand-new organization can transact immediately without first building reference data; a user who doesn't need one deletes or ignores it.

---

## Domain model

**Organization** — the single top-level scope (backend entity `Organization`, keyed by `OrganizationId`). It scopes every other entity; every user and every piece of data belongs to exactly one organization.

**Partner** — a customer, a supplier, or both. Balance = net of receivable and payable, computed per read from the event log — transactions, payments, and _settling_ allocations (TransactionSettlement and AdvanceCredit); ChangeReturn allocations are audit memos and never move the balance — not stored. The first event the balance sums is the partner's opening balance, recorded as an auditable event at creation. Organization setup seeds one ordinary starter partner (rule 42) — not a system actor, fully editable/deletable; the user manages walk-in retail by creating and naming a partner of their own.

**Product** — definition of a sellable / suppliable good: name, SKU, optional description / barcode / category, unit of measurement, packaging details. Carries sale, supply, and retail prices interpreted by ProductType; **retail price is a dormant backend-only field in MVP**. Category is required; the non-null invariant is held by ordinary rules — a category referenced by products cannot be deleted, and a product cannot be created without one. (No protected "default" category exists; organization setup seeds a starter category per rule 42.) Archivable. Holds no quantity — stock lives on InventoryItem.

**Transaction (TransactionRecord)** — an instantaneous, immutable, partner-facing event. Four types: **Sale, Supply, SaleRefund, SupplyRefund**. Has lines (product, quantity, unit price, line discount), a partner (required), a warehouse (`InventoryId`), an optional note, optional attachments, and a date. Refund types require `OriginalTransactionId` pointing to a non-refund transaction of the matching type. A transaction creates a receivable or payable; **payment is a separate, optional event** — entering a transaction does not move money by itself.

**StockAdjustment** — standalone, immutable, audited, partner-less, payment-less stock event with a direction (Decrease | Increase) and a mandatory reason. Decrease = loss (damage / theft / expiry / loss), recorded at WAC and reported as a distinct loss line. Increase = an audited stock-in that restores units (correcting a mistaken decrease, or recording found stock) at current WAC, with no linkage to any prior decrease. _(Replaces the former WriteOff transaction type.)_

**Transfer** — internal stock movement between two warehouses: `FromInventoryId`, `ToInventoryId`, lines, status. Immutable, audited, updates both inventories atomically. No partner, no money.

**Payment** — a partner-level event (not owned by a transaction). Two-sided: source components must equal _settling_ allocations (TransactionSettlement + AdvanceCredit); ChangeReturn allocations sit outside that identity as audit memos.

- **PaymentComponent (source)** — `SourceType ∈ {Wallet, Advance}` + amount. Wallet draws real cash from a wallet; Advance draws against the partner's advance claim.
- **PaymentAllocation (destination)** — `AllocationType ∈ {TransactionSettlement, AdvanceCredit, ChangeReturn}`. Settlement points at a transaction; AdvanceCredit parks money as a partner claim that stays in the wallet; ChangeReturn is a stored audit memo for cash handed straight back, counted in neither wallet nor partner balance.

Payment types: Transaction, Deposit, Withdrawal, Payroll, General. The settlement UI lists outstanding transactions, offers one-click auto-allocation in chronological order, manual per-transaction distribution, or leave-as-advance.

**Advance** — money held for a partner, not tied to a transaction, sourced from overpayment or a standalone Deposit. It is a **claim on cash that physically sits in a wallet**, not a separate money location.

_Worked example (overpayment → advance):_ partner owes 600, pays 1000 cash. One Wallet source component (1000) balances two settling allocations — TransactionSettlement 600 + AdvanceCredit 400. The 400 stays physically in the same wallet, flagged as the partner's claim.
_Worked example (paying from advance + cash):_ partner has a 500 advance claim and hands over 500 cash to settle a 1000 sale. Two source components (Advance 500 + Wallet 500) balance one TransactionSettlement (1000). Settling the advance half touches no wallet.
_Worked example (change returned):_ customer owes 600, hands over 1000 cash, takes 400 back on the spot. One Wallet source component records **600** — the cash the drawer actually kept — balancing one TransactionSettlement (600). A ChangeReturn allocation of 400 is stored as an audit memo only: it enters neither the wallet balance, the rule-8 identity, nor the partner balance. Contrast the overpayment case above: **AdvanceCredit stays in the wallet, ChangeReturn leaves it** — that stay-or-leave fact is the whole reason the two allocation types are distinct.

**Receivable debt** — partner owes business (unpaid Sale or SupplyRefund).
**Payable debt** — business owes partner (unpaid Supply or SaleRefund).

**Wallet** — a money location (`WalletType ∈ {Cash, Card, Bank}`). Balance computed: opening balance + Wallet-type components + inter-wallet transfers. Archivable; an archived wallet still holding money still counts in totals.

**Inventory (warehouse)** — 1–3 per business. Holds InventoryItems. Archivable; an archived warehouse still holding stock still counts in totals.

**InventoryItem** — product + quantity + weighted-average cost, scoped to a warehouse. Sole source of truth for stock.

**Stock-in events** (increase inventory, update WAC): opening stock, Supply, transfer receipt, SaleRefund, StockAdjustment increase.
**Stock-out events** (decrease inventory, leave at WAC): Sale, SupplyRefund, transfer send, StockAdjustment decrease.

**Order** — a pending transaction (customer requested goods; nothing delivered or paid). Lines, customer, optional delivery address, optional requested delivery date, optional source. The write-off warehouse (`InventoryId`) is **chosen at delivery confirmation** (not at creation): the «Подтвердить доставку» dialog picks the source warehouse and validates per-line stock against it before promoting. Auto-promotes to a Sale on **Delivered** using the chosen warehouse. Stock is not reserved while an order is pending; if stock is insufficient at promotion, promotion fails per the negative-stock hard block (rule 20). State machine: Pending → Processing → Shipping → Delivered, with Cancelled / Rejected / Returned branches.

**Template** — a reusable basket of products tied to a specific partner, typed (Sale or Supply, **required**). Stores products only, never prices — prices are calculated live. Loads into a new transaction in one click.

**Employee** — staff of the business, not a system user. Name, position, salary, status, employment date, contact info. Receives Payroll-type payments. An employee may receive any number of Payroll payments per month, including salary advances — there is no one-payment-per-period constraint.

---

## Enum reference

Consolidated. This is the authoritative list.

- **PartnerType:** Customer, Supplier, Both
- **ProductType:** Sale, Supply, All
- **TransactionType:** Sale, Supply, SaleRefund, SupplyRefund
- **TransactionStatus:** Open, Closed, PartiallyPaid, Overdue
- **PaymentType:** Transaction, Deposit, Withdrawal, Payroll, General
- **PaymentDirection:** Income, Expense
- **PaymentSourceType:** Wallet, Advance
- **PaymentAllocationType:** TransactionSettlement, AdvanceCredit, ChangeReturn
- **WalletType:** Cash, Card, Bank
- **StockAdjustmentDirection:** Decrease, Increase
- **OrderStatus:** Pending, Processing, Shipping, Cancelled, Returned, Rejected, Delivered
- **OrderSource:** None, Telegram, OmborWeb
- **TemplateType:** Sale, Supply
- **EmployeeStatus:** Active, Terminated, OnVacation
- **UnitOfMeasurement:** Gram, Kilogram, Ton, Piece, Box, Unit, None

**Removed enums:** `PaymentMethod` (Cash / Card / Bank / AccountBalance) — Cash/Card/Bank move to `WalletType`; AccountBalance becomes the `Advance` source type.
