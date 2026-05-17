# Ombor — project context

**Status:** living document
**Last updated:** 2026-04-20

This document captures what Ombor is, who it's for, and why it's built the way it is. When a design decision is questioned later, the answer should be in this document or added to it. Pair this with the MVP plan for the "what."

---

## What we're building

Ombor is a warehouse and business management system for small businesses in Uzbekistan that currently track their operations on paper or in Excel. It replaces the notebook with a reliable system that tracks products, partners, transactions, payments, inventory, and outstanding debts — with an audit trail strong enough to settle disputes.

The product is web-first with a read-only mobile companion. It's trilingual (Uzbek Latin, Uzbek Cyrillic, Russian).

---

## Who it's for

Small businesses that buy and sell goods. Specifically, businesses that:

- Buy from suppliers and sell to customers (resale) or produce in small batches (production is not our problem to solve — they track their own)
- Operate partly on BNPL / credit with their customers and suppliers
- Currently use paper notebooks or Excel spreadsheets to track transactions
- Deal in UZS natively, occasionally in USD
- Have 1 to 3 warehouses or storage locations
- Are not part of the formal e-invoicing ecosystem (no e-faktura, no Soliq integration needed)

Examples of the target shape:

- Small supermarket
- Home-based shop selling daily goods
- Bazaar shop
- Ice-cream reseller buying regionally and selling locally
- Nut/fruit exporter buying domestically and selling abroad
- Furniture reseller buying from factories and selling retail
- Vitamin importer buying from USA and reselling locally

We don't care about segment-specific edge cases (telegram order integration, export invoicing, etc.). Every segment shares the same core need: track what comes in, what goes out, who owes whom, and what's in stock.

Businesses that are **not** our target:
- Anyone requiring e-faktura or formal tax integration
- Large retailers who've outgrown simple ERP
- Production-heavy businesses needing BOM and raw-material tracking
- Businesses with foreign-currency-denominated contractual debts (possible v2 expansion)
- Walk-in consumer customers of our customers (they're not users; they appear as anonymous full-payment sales)

---

## Design partners

Three businesses committed to beta feedback during MVP build and early use:

1. An ice-cream reseller/small factory — buys in bulk from other regions, resells locally, sells both B2B to smaller shops and B2C walk-ins
2. A vitamin importer — buys from the US, resells in Uzbekistan
3. A furniture reseller — buys from factories, sells in retail store

Together these cover: regional-distribution reselling, cross-border import reselling, and B2C retail reselling. A reasonable spread across the target shape.

No public launch until these three have used the product in real operations for at least four weeks and provided structured feedback.

---

## Market positioning

The competitive landscape sits in four zones:

1. **Enterprise accounting (1C and variants)** — dominant for mid-size businesses, requires a trained accountant, desktop-heavy, expensive, strong compliance. Overkill and alienating for our target users.

2. **POS-first retail (Billz, Kassa.uz, Payme POS)** — good at checkout, barcodes, fiscal receipts, multi-store inventory. Weak on B2B credit management, supplier-side workflows, and non-retail reporting.

3. **Cloud inventory/ERP (MySklad and similar)** — broader than Billz. Usually Russian-only UI, priced for slightly larger businesses, no Uzbek-specific affordances.

4. **Paper and Excel** — zero cost, no learning curve, total flexibility. Wins on familiarity; loses on audit trail, dispute verification, reporting, shareability. This is our real competitor.

Ombor sits between zones 2 and 4: simpler than 1C and MySklad, broader than Billz (since we handle supplier-side workflows and B2B credit), and dramatically better than Excel at BNPL audit trails.

**Sharpest differentiator:** the audit trail for credit disputes. No competitor treats this as a first-class concern. It's the specific pain the product was conceived around.

---

## Core design decisions and reasoning

### Target: informal, not formal

We don't support e-faktura, Soliq, or Didox. Our users operate outside or adjacent to the formal invoicing system — that's precisely why they need us. Chasing compliance would dilute the product and delay shipping. This is a long-term positioning choice, not just a v1 deferral.

### Web-first, mobile read-only

Full transaction entry, partner management, and reporting live on the web. Mobile is a read-only companion for when the owner is away from their computer and needs to check a balance or look up a partner. Mobile write flows are deferred to v2 so we don't split design effort before the core flows are solid.

### Single-user per business for MVP; multi-user + roles in v2

Building a speculative role system now would mean designing against imagined usage. We'll see real usage patterns from the three design partners and design multi-user correctly in v2.

### No POS receipt printing in MVP

Our users need a good transaction-entry surface (the place where they spend most of their time), but they don't need thermal printers, cash drawers, or fiscal receipt compliance in v1. Adding those would put us in direct competition with Billz on their strongest surface. Deferred pending user demand from design partners.

### Transactions and payments are immutable

Corrections are made via reverse events, never by editing. This protects the audit trail — which is the differentiator — from the kind of quiet tampering that would make it worthless for dispute resolution.

### Payment allocations are exposed, with FIFO as a one-click path

Every payment can be allocated across specific outstanding transactions, or kept as partner advance. The rationale: when a partner disputes their balance, the user can point to exactly which payments settled which transactions. Auto-allocation in transaction order is available as a one-click shortcut so users don't have to allocate manually for routine cases.

We considered two alternatives and rejected them:
- Pure running-balance ledger with no allocation — simpler but loses per-transaction settlement detail for disputes
- Mandatory manual allocation on every payment — too much friction for casual BNPL

The exposed allocation model with a skip path (leave as advance) and a FIFO shortcut balances auditability with ease of use.

### Multi-currency as capture, not native

UZS is the internal currency. USD is captured at the exchange rate on the event date, stored as UZS-equivalent. This works for users who occasionally accept USD but think and price in UZS. It does not work for users with genuine USD-denominated contracts — that's a v2 feature when we decide to serve that segment (e.g., the nut/fruit exporter).

Exchange rate defaults to CBU's daily rate but is user-overridable because CBU's rate doesn't always match the market rate users actually transact at.

### Weighted-average cost for profit

Without a real cost basis, "profit" numbers are misleading — users track "revenue minus last purchase price" in Excel and are systematically wrong when supply prices drift. Weighted-average cost gives the true figure and is the specific win our system delivers over paper/Excel tracking. Stored as a field on InventoryItem, updated atomically on every stock-in.

### Opening balances as auditable events

When a new business onboards, they already have partners who owe them and stock on shelves. Opening balance and opening stock are proper ledger events with who/when metadata, not raw starting numbers. This closes the one weak link in an otherwise complete audit trail.

### Narrow audit scope

Audit applies to events affecting money or stock. Non-financial edits (product name typo, partner phone update) are not audited in MVP. A full audit framework is deferred — the narrow scope is what the dispute use case actually needs.

### Partner balance computed, not stored

Balance is derived per read from the event log (via a database view). Stored balances drift when jobs crash mid-transaction. At MVP volumes this is free, and it eliminates a whole class of bugs.

### Hard-block negative stock in v1

Real shops often sell first and reconcile later, but allowing negative stock in MVP would complicate the inventory model significantly and confuse the profit numbers. We start strict; if design partners push back, we revisit.

### Archive, not delete

Products and partners can be archived but not hard-deleted once referenced by any transaction. Hard deletion would orphan history and break audit trails.

---

## Key product concepts (glossary)

**Partner** — any customer, supplier, or both. A single entity in the system regardless of role. A partner's balance reflects the net of what they owe us and what we owe them.

**Transaction** — an instantaneous, immutable event: Sale, Supply, SaleRefund, SupplyRefund, or WriteOff. Transactions affect inventory and (except WriteOff) partner balance.

**Payment** — a partner-level event representing money moving between business and partner. Payments have types (Transaction, Deposit, Withdrawal, Payroll, General) and components (method, currency, amount, exchange rate).

**Allocation** — the link between a payment and one or more transactions it settles. Also covers advance-payment allocations (credit held for future use) and change returns.

**Advance** — money held on a partner's behalf, not tied to any specific transaction. Can be credited from overpayment (partner paid more than they owed) or a standalone deposit (partner sent money to be used later).

**Payable debt** — money the business owes the partner (from unpaid supplies or sale refunds).

**Receivable debt** — money the partner owes the business (from unpaid sales or supply refunds).

**Stock-in event** — any event that increases inventory: opening stock, supply transaction, transfer receipt, refund-from-customer.

**Stock-out event** — any event that decreases inventory: sale transaction, write-off, transfer send, refund-to-supplier.

**Template** — a reusable basket of products tied to a specific partner. Loads into a new transaction in one click.

**Order** — a pending transaction. Customer has requested goods but nothing has been delivered or paid. Promotes to a Sale transaction when delivered.

**Organization** — a single tenant in the system. Every user and every piece of data belongs to exactly one organization.

---

## Explicit non-goals

What Ombor will never do (unless the strategy changes fundamentally):

- Replace accountants or tax software
- Serve formal-sector businesses with compliance obligations
- Compete feature-for-feature with Billz on POS
- Compete feature-for-feature with 1C on accounting
- Handle manufacturing, BOM, or multi-stage production
- Be an HR system (payroll is a simple payment flow, not HR)
- Be an e-commerce platform (orders are internal, not a public-facing shop)

---

## Open strategic questions (living list)

Things not yet decided but will need decisions as the product grows:

- When does it make sense to add true multi-currency? (Tied to whether we go after the exporter segment.)
- When does it make sense to build e-faktura integration? (Tied to whether we go after the formal SME segment — currently "probably never.")
- Pricing model for the product itself — flat monthly, per-user, per-transaction, free-with-paid-features?
- Onboarding assistance — self-serve wizard, white-glove for early users, import from 1C?
- Mobile write flows — when and in what order?

These don't need answers now. They need to be revisited before v2 planning.