# Ombor — Claude context

Reference document for Claude conversations about this project. Dense by design. Pair with mvp-plan.md for feature scope and acceptance criteria.

---

## Product

Warehouse and business management system for small businesses in Uzbekistan. Replaces paper notebooks and Excel. Web-first with read-only mobile companion. Trilingual: UZ-Latin, UZ-Cyrillic, RU.

**Sharpest differentiator:** audit trail for BNPL credit disputes. Not e-faktura compliance, not POS polish — dispute-grade ledger.

**Real competitor:** Excel and paper. Not 1C, not Billz.

---

## Target user shape

Small businesses that buy and resell, with 1-3 warehouses, operating partly on credit, currently using paper or Excel. Examples: small supermarket, home shop, bazaar shop, regional reseller, import reseller, retail reseller.

**Not target:** formal-sector businesses needing e-faktura; production-heavy businesses needing BOM; businesses with foreign-currency-denominated contracts; large retailers.

---

## Tech stack

- Backend: .NET + SQL Server (EF Core)
- Web: React + TypeScript + Material UI
- Mobile: React Native (read-only in v1)

---

## Current state (as of 2026-04-20)

Scope locked, execution pending. Backend has most CRUD for products, partners, transactions, payments, inventory, orders, templates, employees/payroll, auth. Frontend has core pages. Mobile is read-only.

**Known gaps before MVP can ship** (tracked separately in tech change list):
- Multi-tenancy enforcement — done (2026-05-17): `TenantId` on all tenant-scoped entities, JWT-fed tenant accessor, EF Core global query filters
- Weighted-average cost not implemented
- WriteOff transaction type missing
- Inter-warehouse transfers missing
- Transaction-to-warehouse linkage missing
- Archive (soft-delete) not implemented; DELETE endpoints are hard-delete
- Reverse mechanism for transactions/payments not implemented
- Opening balance/stock stored as raw numbers, not auditable events
- Cyrillic↔Latin search not implemented
- Server-side pagination/filtering/search not implemented
- Product.QuantityInStock still writable (dual source of truth bug with InventoryItem)
- PUT/DELETE on payrolls needs removal (violates payment immutability)

---

## Locked design decisions

**Multi-tenancy** — every tenant-scoped entity carries `TenantId` and implements `ITenantScoped`. Enforced (since 2026-05-17) via an EF Core global query filter; the DbContext stamps `TenantId` on insert. The tenant is resolved from a `tenant_id` JWT claim by `ITenantAccessor`. The tenant entity itself is `Tenant` (formerly `Organization`).

**Transactions and payments are immutable.** Corrections via reverse events only. Payroll follows the same rule — no PUT/DELETE.

**Payment allocation is exposed to the user**, not hidden. Options: manual per-transaction, FIFO one-click shortcut, or leave as advance. Rejected alternatives: pure running balance (loses dispute detail), mandatory manual (too much friction).

**Partner.Balance is computed via DB view**, not stored. Stored balance drifts on crash; computed doesn't.

**InventoryItem is sole source of truth for stock.** Product.QuantityInStock is deprecated and must be removed — do not write to it.

**Weighted-average cost stored on InventoryItem**, updated atomically on stock-in. Not a view (sequential calculation, not aggregable).

**Opening balance and opening stock are auditable ledger events**, not raw number assignments. Recorded with who/when metadata.

**Narrow audit scope:** events affecting money or stock are immutable and audited. Non-financial edits (product name, partner phone) not audited in MVP.

**Currency:** UZS-native, USD as capture. Exchange rate stored per event. Rate defaults to CBU daily, user-overridable. Not true multi-currency — partner balances are always UZS.

**Negative stock:** hard-blocked in v1. Revisit if design partners push back.

**Single-user per business** in MVP. Multi-user + roles together in v2. Don't speculatively design roles now.

**Hard-delete:** not allowed on entities with history. Archive instead. Applies to Product and Partner minimum.

**No POS receipt printing** in MVP (not competing with Billz on their strength). Transaction entry UX still critical — that's where users spend most time.

**Discounts:** percentage or fixed amount, per line and per transaction.

**Attachments** on transactions and payments. Already implemented.

---

## Domain model essentials

**Partner** — customer, supplier, or both. Balance = net of what they owe and what we owe. Computed from event log, not stored.

**Transaction** — immutable event. Types: Sale, Supply, SaleRefund, SupplyRefund, WriteOff (WriteOff has no partner). Has lines, warehouse, optional note, optional attachments, optional inline payments. Reverse via refund-type transactions.

**Payment** — partner-level event (not owned by transaction). Types: Transaction, Deposit, Withdrawal, Payroll, General. Has components (method/currency/amount/rate — multiple allowed) and allocations (optional transaction links). Immutable.

**Allocation** — payment→transaction link, or advance-payment marker, or change-return marker. Allocation types: Sale, Supply, SaleRefund, SupplyRefund, AdvancePayment, ChangeReturn.

**Advance** — money held for a partner, not tied to a transaction. From overpayment or standalone Deposit. Distinct from debt.

**Payable debt** — business owes partner (unpaid Supply or SaleRefund).
**Receivable debt** — partner owes business (unpaid Sale or SupplyRefund).

**Order** — pending transaction. Promotes to Sale on delivery. State machine: Pending → Processing → Shipping → Delivered, with Cancelled/Rejected/Returned branches.

**Template** — reusable basket of products for a specific partner. Loads into new transaction in one click.

**Inventory** — warehouse (1-3 per business). Holds InventoryItems (product + quantity + weighted-avg cost).

**Stock-in event** — increases inventory: opening stock, Supply, transfer receipt, SaleRefund.
**Stock-out event** — decreases inventory: Sale, WriteOff, transfer send, SupplyRefund.

**Employee** — staff of the business, not system user. Has salary, status, contact info. Receives Payroll-type payments.

**Tenant** — single tenant (entity named `Tenant`). Every user and every piece of data belongs to exactly one.

---

## Key enums (from current schema)

- `PartnerType`: Customer, Supplier, Both
- `ProductType`: Sale, Supply, All
- `TransactionType`: Sale, Supply, SaleRefund, SupplyRefund, WriteOff
- `TransactionStatus`: Open, Closed, PartiallyPaid, Overdue
- `PaymentType`: Transaction, Deposit, Withdrawal, Payroll, General
- `PaymentDirection`: Income, Expense
- `PaymentMethod`: Cash, Card, Bank, AccountBalance
- `PaymentAllocationType`: Sale, Supply, SaleRefund, SupplyRefund, AdvancePayment, ChangeReturn
- `OrderStatus`: Pending, Processing, Shipping, Cancelled, Returned, Rejected, Delivered
- `OrderSource`: None, Telegram, OmborWeb
- `TemplateType`: Sale, Supply
- `EmployeeStatus`: Active, Terminated, OnVacation
- `UnitOfMeasurement`: Gram, Kilogram, Ton, Piece, Box, Unit, None

---

## Non-goals

Will never build unless strategy fundamentally changes:

- E-faktura / Soliq / Didox / 1C integration
- POS receipt printing, thermal printers, cash drawers, fiscal memory
- Manufacturing, BOM, multi-stage production
- HR system (payroll is a simple payment flow, not HR)
- E-commerce platform (orders are internal, not public-facing)
- Accountant replacement, tax software

Deferred to v2+ (possibly):
- True multi-currency, receipt printing, production, batch/lot/expiry, variants, per-partner pricing, multi-register cash boxes, offline, English, credit limits, aging buckets, barcode scanning, mobile write flows, multi-user+roles, in-place editing, data import/export, Telegram bot, general audit framework.

---

## Design partners

Three committed: ice-cream reseller, vitamin importer (USA→UZ), furniture reseller. No public launch until all three have used the product for 4+ weeks with structured feedback.

---

## When in doubt

- Err toward simplicity over flexibility
- Err toward audit integrity over UX convenience
- Err toward "defer to v2" over "add to MVP"
- Err toward computed fields over stored fields
- Check mvp-plan.md for feature scope; check this doc for decisions and constraints
