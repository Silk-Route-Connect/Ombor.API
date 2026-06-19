# Ombor — MVP plan

**Status:** canon. Rebuilt 2026-06-11, reconciled against `business-rules.md` (2026-06-11) and the completed Design refactoring. Supersedes the April mvp-plan (which predated wallets, the payment source/allocation model, StockAdjustment, multi-user, and UZS-only).
**Last updated:** 2026-06-11

Defines what ships in v1. Anything not listed is deferred — adding to this list requires an explicit decision (rule 36). Behavior is specified by `business-rules.md`; reasoning by `product-brief.md`; this doc only states scope and done-ness. When this doc and a rule conflict, the rule wins.

**Launch gate:** all three design partners using the product in real operations for ≥4 weeks, structured feedback reviewed. No public launch before that.

---

## Cross-cutting requirements (apply to every module)

- **Trilingual-ready:** all UI strings through i18n keys; ru complete at implementation time; uz-Latn and uz-Cyrl backfilled in one pass **before partner rollout** (this backfill is a launch blocker).
- **UZS-only** (rule 33); all amounts via shared currency formatting.
- **Multi-user, no roles** (rule 35); accountability via audit.
- **Immutability UX:** no edit/delete affordances on transactions, payments, payroll, adjustments, transfers; correction flows instead (rule 1).
- **Computed balances served by the backend** (rule 12).
- **Hard-block negative stock** (rule 20).
- **Archive, never delete** for Product, Partner, Wallet, Warehouse (rules 29–32).
- Designed screens implemented per the frontend `docs/design-handoff.md` locked patterns.
- List pages carry an «Экспорт» action (title level): client-side CSV of the current filtered/sorted view. No backend involvement; distinct from the cut org-wide data export.

---

## v1 modules

### 1. Auth

Registration, login, OTP verification, password reset. Multi-user per tenant.
_Done:_ a business registers, verifies, logs in; a second user joins the same tenant; a user resets a forgotten password. _(Design: built — two-panel surface; OTP 4-digit; register collects Имя+Фамилия, email/Telegram dropped; password reset mocked pending backend.)_

### 2. Dashboard

KPI cards (revenue, debt totals, stock value, wallet cash), debt aging buckets, time-series chart with period controls.
_Done:_ numbers reconcile with the underlying module pages for the same period. Analytics beyond this is v2 (Reports module).

### 3. Products & Categories

Product CRUD per Domain model (prices interpreted by type; retail price dormant — no UI). Multiple images. Archive/restore. Search by name/SKU, Cyrillic↔Latin parity. Category CRUD; a starter category is seeded at tenant setup (rule 42, ordinary entity). The product form pre-selects the category when the tenant has exactly one. 1–3 warehouses; per-warehouse stock (InventoryItem: quantity + WAC); opening-stock entry flow (audited stock-in with unit costs — rule 22); archive with totals preserved (rule 31).
_Done:_ full lifecycle incl. archive; WAC visible and correct after supplies at different prices.

### 4. Warehouses & stock view

1–3 warehouses; per-warehouse stock (InventoryItem: quantity + WAC); archive with totals preserved (rule 31).
_Done:_ stock view matches event history per warehouse.

### 5. Stock Adjustments

Decrease (loss: damage/theft/expiry) and Increase (correction/found stock), mandatory reason, immutable, WAC rules 23–25.
_Done:_ a Decrease shows as a loss line distinct from COGS; negative stock impossible.

### 6. Transfers

Between-warehouse moves, atomic, immutable, audited.
_Done:_ both warehouse balances correct after transfer; transfer visible in both histories.

### 7. Partners

CRUD per Domain model; opening balance as one-time immutable event at creation; full-page detail with ledger (chronological event log explaining every balance change), balance with natural-language label; archive/restore. System partner «Розничный покупатель» present, non-editable (rule 39).
_Done:_ any partner balance is fully explainable from the visible ledger.

### 8. Sales & POS (New Sale)

Sale entry with lines, line-level discounts (% or fixed, rules 37–38), warehouse, partner, notes, attachments; POS-style New Sale defaulting to the walk-in partner; integrated payment section; overpayment settlement modal (change return default, settle-other-debts opt-in, advance only at zero debt — rule 40).
_Done:_ a credit sale creates a receivable; a paid sale settles via correct allocations; stock decremented at WAC.

### 9. Supplies (New Supply)

Mirror of sales on the supplier side; stock-in updates WAC atomically.
_Done:_ an unpaid supply creates a payable; WAC recomputes correctly.

### 10. Refunds

SaleRefund / SupplyRefund per rules 2–7: original linkage, type match, per-line remaining-refundable, mandatory reason, multiple partial refunds.
_Done:_ over-refund impossible; refunds adjust stock and balances correctly.

### 11. Orders

Order lifecycle per Domain model state machine; warehouse chosen at delivery confirmation; auto-promotion to Sale on Delivered; no stock reservation while pending.
_Done:_ a Delivered order becomes a Sale against the chosen warehouse; promotion fails cleanly on insufficient stock.

### 12. Templates

Typed (Sale | Supply) per-partner product baskets, prices always live; one-click load into a new transaction.
_Done:_ template load fills lines with current prices.

### 13. Payments & Debts

Standalone payment flow (types per rule 13–14), settlement UI (auto-allocate chronologically / manual / leave-as-advance), payment detail showing components and allocations; Debts page: receivable/payable overview with aging; advances per rules 10–12, 40.
_Done:_ every payment's sources equal its settling allocations; a disputed balance can be traced payment-by-payment to the transactions it settled.

### 14. Wallets

Cash/Card/Bank wallets; computed balances; opening balance event; inter-wallet transfers; "our money" (balance − advances held) displayed; archive with totals preserved.
_Done:_ wallet totals reconcile with payment history; advance claims visible.

### 15. Employees & Payroll

Employee CRUD; Payroll payments (immutable, always Expense), any number per month including advances; payroll history per employee.
_Done:_ two payroll payments in one month both record and report correctly.

### 16. Акт сверки (reconciliation statement)

Per-partner, date-range, print-friendly statement rendered from the ledger: transactions, payments, running balance, closing balance; exported via browser print-to-PDF. No custom PDF templating, no two-sided act format. _(No Design prototype needed — print layout.)_
_Done:_ statement matches the on-screen ledger for the same range.

### 17. Activity Log

Single audit screen per rule 28: filter by entity, actor, date. _(Design gap — see Open items.)_
_Done:_ a master-data edit and a money event both appear with actor and before/after values.

### 18. Settings

Business profile (name, address, phone, email, logo); per-user interface language (ru / uz-Latn / uz-Cyrl — the header globe switches the same per-user setting); informational read-only currency section (UZS, rule 33); user management: invite user, deactivate/reactivate per rule 41 (never delete), cosmetic «Администратор» chips with the no-roles footnote. No data-export feature (cut — see Deferred).
_Done:_ a second user joins and works; a deactivated user cannot log in but stays attributed in the audit log; a language change applies only to the user who made it.

---

## Deferred (explicit, with destination)

- **Reports module** (profit/analytics/trends) — **v2**; dashboard KPIs are the only v1 analytics surface.
- **Roles & permissions** — v2 (rule 35).
- **Multi-currency** — dedicated future effort (rule 33).
- **POS receipt printing / fiscal hardware** — pending design-partner demand.
- **Mobile read-only companion** — post-MVP; v1 is web-only.
- **Приёмка (goods acceptance) as a standalone entity** — cut; supplies are recorded directly.
- **Negative stock allowance** — revisit on design-partner pushback.
- **Cycle-count / physical-inventory reconciliation; Decrease↔Increase linkage** — v2 (rules 24–25).
- **Telegram order capture; e-commerce surfaces** — out (enum `OrderSource.Telegram` stays dormant).
- **Onboarding / data import wizard** — required before public launch, scoped separately; not a design-partner blocker.
- **Retail-price features** — dormant field only.
- **Cashbox machinery** (shift reconciliation, register counts) — deferred.
- **Org-wide data export (full CSV archive, Settings)** — cut from MVP; revisit at v2 planning. Other exports are acceptable.
- **Currency machinery cleanup** — post-MVP (frozen meanwhile).

---

## Open items

1. **Activity Log screen has no Design prototype** — still v1 scope; the small Design session for it is deliberately deferred.
2. **Auth design review pending.**
3. **Sidebar simplification** — section-label tier removed and «Отчёты» dropped until v2 (decision 2026-06-11); Design System README + Settings prototype fix prompted; implementation follows the design-handoff sidebar spec, which supersedes sidebars drawn in existing prototypes.
4. **Uzbek backfill pass** — scheduled before partner rollout; owner: Miraziz review per string.
