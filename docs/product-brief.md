# Ombor — product brief

**Status:** canon. Consolidates and supersedes `project-context.md` and the vision / positioning / reasoning content of `claude-context.md`; both retire.
**Last updated:** 2026-06-11

The "why" document — vision, who it serves, how it's positioned, and the reasoning behind the decisions that shaped it. When a design decision is questioned later, the answer should be here or added here. Pair with `business-rules.md` for the spec the implementation obeys and `mvp-plan.md` for what ships in v1. When this doc and a rule conflict, the rule wins — raise the conflict.

---

## What we're building

Ombor is a warehouse and business-management system for small businesses in Uzbekistan that run their operations on paper or in Excel. It replaces the notebook with a system that tracks products, partners, transactions, payments, inventory, cash, and outstanding debt — with an audit trail strong enough to settle a dispute.

Web-first, with a read-only mobile companion. Trilingual: Uzbek Latin, Uzbek Cyrillic, Russian, with Cyrillic↔Latin search parity so a name typed either way finds the same record.

---

## Who it's for

Small businesses that buy and sell goods. Specifically, businesses that:

- Buy from suppliers and sell to customers (resale); small-batch production is not our problem to solve
- Operate partly on BNPL / credit with their customers and suppliers
- Currently track everything on paper or in Excel
- Deal in UZS
- Have 1 to 3 warehouses or storage locations
- Are outside the formal e-invoicing ecosystem (no e-faktura, no Soliq integration needed)

Examples of the target shape: a small supermarket, a home-based daily-goods shop, a bazaar shop, an ice-cream reseller buying regionally and selling locally, a furniture reseller buying from factories and selling retail, a vitamin importer buying abroad and reselling locally.

Every segment shares the same core need: track what comes in, what goes out, who owes whom, what's in stock, and where the cash is. We ignore segment-specific edge cases (Telegram order capture, export invoicing, etc.).

**Not our target:**

- Anyone requiring e-faktura or formal tax integration
- Large retailers who've outgrown simple tooling
- Production-heavy businesses needing BOM and raw-material tracking
- Businesses with foreign-currency-denominated contractual debts (possible later expansion)
- The walk-in consumers of our customers — they aren't users; they appear as sales linked to whatever partner the business chooses to represent walk-in trade

---

## Design partners

Three businesses committed to close feedback during MVP build and early use. ("Design partner" is the startup sense — early customer, not a UI/UX designer.)

1. **Ice-cream reseller / small factory** — buys in bulk from other regions, resells locally, sells both B2B to smaller shops and B2C to walk-ins.
2. **Vitamin importer** — buys from the US, resells in Uzbekistan.
3. **Furniture reseller** — buys from factories, sells in a retail store.

Together they cover regional-distribution reselling, cross-border import reselling, and B2C retail — a reasonable spread across the target shape. No public launch until all three have used the product in real operations for at least four weeks and given structured feedback that's been reviewed.

---

## Market positioning

The landscape sits in four zones:

1. **Enterprise accounting (1C and variants)** — dominant for mid-size businesses; needs a trained accountant, desktop-heavy, expensive, strong compliance. Overkill and alienating for our users.
2. **POS-first retail (Billz, Kassa.uz, Payme POS)** — strong at checkout, barcodes, fiscal receipts, multi-store inventory. Weak on B2B credit, supplier-side workflows, and non-retail reporting.
3. **Cloud inventory / ERP (MySklad and similar)** — broader than Billz, usually Russian-only UI, priced for larger businesses, no Uzbek-specific affordances.
4. **Paper and Excel** — zero cost, no learning curve, total flexibility. Wins on familiarity; loses on audit trail, dispute verification, reporting, shareability. **This is our real competitor.**

Ombor sits between zones 2 and 4: simpler than 1C and MySklad, broader than Billz (we handle supplier-side workflows and B2B credit), and dramatically better than Excel at credit audit trails.

**Sharpest differentiator:** the audit trail for credit disputes. No competitor treats it as a first-class concern. It is the specific pain the product was conceived around.

---

## Core design decisions and reasoning

### Target informal, not formal

We don't support e-faktura, Soliq, or Didox. Our users operate outside or adjacent to the formal invoicing system — that's precisely why they need us. Chasing compliance would dilute the product and delay shipping. A long-term positioning choice, not a v1 deferral.

### Web-first, mobile read-only

Full entry, partner management, and reporting live on the web. Mobile is a read-only companion for checking a balance or looking up a partner away from the computer. Mobile write flows are deferred so we don't split design effort before the core flows are solid.

### Multi-user in MVP, accountability via audit, roles deferred

Multiple staff in one business can all use the system; there are no roles or permission tiers in v1. The thing that makes this safe without a role system is the audit log: every money/stock event and every master-data edit is attributed to an actor, so accountability is achieved by _recording who did what_ rather than by _restricting who can do what_. Building a speculative permission system now would mean designing against imagined usage; we'll see real patterns from the three design partners and design roles correctly later. (This reverses the earlier "single-user in MVP" stance — multi-use turned out to be a day-one reality for these businesses, and audit-as-accountability removes the need to gate it behind roles.)

### Wallets (money locations) in MVP

A business needs to answer "how much cash do I have, and where" — drawer, card, bank. So a wallet is a first-class money location (Cash / Card / Bank), and every payment moves money through a wallet or against a partner's advance. This was previously deferred as "multi-register cash boxes," but it turned out to be inseparable from the payment model: a payment has to come _from_ somewhere, and that somewhere is a wallet. What stays deferred is heavier cashbox machinery (shift reconciliation, register-level cash-counts) — the MVP just needs accurate per-wallet balances.

### Two-sided payments: source and allocation

Every payment is balanced — a **source** side (which wallet the money moved through, or a draw against the partner's advance) and an **allocation** side (which transaction it settled, money parked as advance, or change returned). Sources must equal allocations. The rationale is the differentiator: when a partner disputes their balance, the user can point to exactly which money settled which transaction, and exactly where it came from. Allocation is exposed to the user, with a one-click chronological auto-allocate as the shortcut for routine cases and a leave-as-advance skip path. We rejected a pure running-balance ledger (loses per-transaction settlement detail for disputes) and mandatory manual allocation on every payment (too much friction).

### Advance is a claim, not a location

An advance is money held on a partner's behalf — from an overpayment or a standalone deposit. Physically that cash sits in a wallet; "advance" is a claim _on top of_ the real wallet cash, not a separate pot. "Our money" in a wallet is therefore its balance minus advances held, computed per read. This keeps a single physical truth (the wallet) while still tracking what's owed back.

### Walk-in retail via a system partner

Every sale needs a partner for the ledger to stay uniform and disputes to stay traceable, so we don't allow anonymous sales — but we also don't impose a system "walk-in" actor. At setup we seed one ordinary partner (alongside a wallet, warehouse, and category) so a new business can transact on day one; it's editable, renamable, and deletable like any record the user creates. A business that does frequent walk-in retail names a partner for it («Розничный покупатель», «Не сохранённый клиент», whatever fits); one that doesn't, ignores or deletes the seeded one. Forcing an explicit partner choice on every sale is deliberate — it prevents un-attributed transactions and pushes the user to name counterparties in a way that makes their own audit trail more legible.

### Single currency in MVP; multi-currency is a later, dedicated effort

The MVP is UZS-only — no per-entity currency, no exchange rates, no rate source. We pulled USD-capture out of scope entirely (it was previously planned as "capture at event rate"). A half-built capture model creates phantom balance drift for anyone with genuine foreign-currency debts and bakes assumptions that a real multi-currency build would have to unwind. When we decide to serve the exporter segment, multi-currency gets its own design pass with all the cascade cleanups, rather than being approximated now.

### No POS receipt printing in MVP

Users need a good transaction-entry surface — that's where they spend most of their time — but not thermal printers, cash drawers, or fiscal-receipt compliance in v1. Adding those would put us head-to-head with Billz on their strongest surface. Deferred pending design-partner demand.

### Transactions and payments are immutable

Corrections are made via reverse / counter events, never by editing. This protects the audit trail — the differentiator — from the quiet tampering that would make it worthless for dispute resolution. Master data (product name, partner phone) remains editable, and those edits are logged; immutability applies to the money/stock events themselves.

### Weighted-average cost for profit

Without a real cost basis, profit numbers mislead — Excel users track "revenue minus last purchase price" and are systematically wrong when supply prices drift. Weighted-average cost gives the true figure and is a specific win over paper. Stored on InventoryItem, updated atomically on every stock-in.

### StockAdjustment instead of a WriteOff transaction type

Stock leaving for a non-sale reason (damage, theft, expiry, loss) — or returning to correct a mistaken decrease, or recording found stock — is a standalone, partner-less, payment-less, audited stock event with a direction (Decrease / Increase) and a mandatory reason. It is deliberately _not_ a transaction type: it touches no partner and moves no money, so modelling it as a transaction would have forced awkward partner/payment nullability everywhere. A Decrease is reported as a distinct loss line, separate from COGS, so real losses don't hide inside cost of goods.

### Opening balances and opening stock as auditable events

A new business already has partners who owe them and stock on shelves. Opening balance and opening stock are proper ledger events with who/when metadata, not raw starting numbers — closing the one weak link in an otherwise complete audit trail.

### Audit scope: money/stock events plus master-data CRUD, attributed

Every money/stock event is immutable and attributed. On top of that, create/edit/archive/delete on the mutable master data (products, partners, wallets, warehouses, employees, templates, orders) is logged too. The wider-than-"money-only" scope is driven by multi-user: with several people editing shared data and no roles, you have to be able to see who changed a partner's phone or archived a product, not just who moved money. It is still a targeted scope, not a general-purpose audit framework — and logging an edit never makes an immutable event editable.

### Partner and wallet balances computed, not stored

Both are derived per read from the event log. Stored balances drift when a job crashes mid-write; computed balances can't. At MVP volumes this is free and eliminates a whole class of bugs.

### Hard-block negative stock in v1

Real shops sometimes sell first and reconcile later, but allowing negative stock in MVP would complicate the inventory model and confuse profit numbers. We start strict; if design partners push back, we revisit.

### Archive, not delete

Products, partners, wallets, and warehouses can be archived but never hard-deleted once referenced. Archiving is never blocked by existing references, and an archived wallet or warehouse that still holds money or stock still counts in totals — hiding it from pickers must not hide its value. Hard deletion would orphan history and break the audit trail.

### Retail price dormant in MVP

A product carries sale, supply, and retail prices, but retail is a backend-only dormant field in v1 — present in the schema, absent from the UI and flows. It's retained now so adding retail-price features later doesn't require a migration, but it earns no MVP surface area because none of the launch flows need it.

---

## Explicit non-goals

What Ombor will not do unless the strategy changes fundamentally:

- Replace accountants or tax software
- Serve formal-sector businesses with compliance obligations
- Compete feature-for-feature with Billz on POS, or with 1C on accounting
- Handle manufacturing, BOM, or multi-stage production
- Be an HR system (payroll is a simple payment flow, not HR)
- Be an e-commerce platform (orders are internal, not a public-facing shop)

---

## Open strategic questions

Not decided; revisit before v2 planning:

- **Roles and permissions** — when does real usage justify them, and what granularity? (Multi-user ships in MVP without them; audit carries accountability until then.)
- **True multi-currency** — tied to whether we pursue the exporter segment.
- **E-faktura / formal integration** — tied to whether we ever serve the formal SME segment (currently "probably never").
- **Pricing model** — flat monthly, per-user, per-transaction, free-with-paid-features?
- **Onboarding and data import** — self-serve wizard, white-glove for early customers, import from 1C/Excel? (Flagged critical before public launch; scope is a separate discussion.)
- **Mobile write flows** — when, and in what order?
