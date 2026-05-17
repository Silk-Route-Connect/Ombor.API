# Ombor planning

Live document. Updated as phases progress. Master "where are we" reference.

---

## Current phase

**Phase 1 — Audit complete.** Codebase audited against `rules.md` and `claude-context.md`; confirmed gaps recorded in `tech-change-list.md`. Ready to begin Phase 3 (Build).

---

## Phase status

### Phase 0 — Foundation

- [x] Locked design decisions → `claude-context.md`
- [x] MVP scope → `mvp-plan.md`
- [x] Hard rules → `rules.md`
- [x] Tech change list template (with known gaps) → `tech-change-list.md`
- [x] CLAUDE.md entry point
- [x] Project context → `project-context.md`
- [ ] Multi-tenancy implementation pattern confirmed (resolves in Phase 1)
- [ ] SQL Server backup strategy defined (before first customer)

### Phase 1 — Audit existing code

**Complete (2026-05-17).** Executed in Claude Code. Output landed in `tech-change-list.md` — see the "Added by Phase 1 audit" and "Pre-production hardening" sections. All gaps predicted by the initial review were confirmed; module-level detail and exact code locations added.

**Goal:** assess every existing module against `rules.md` and `claude-context.md`. Output: `tech-change-list.md` filled with confirmed gaps and severity.

**Modules to audit:**
- Products + Categories
- Partners
- Sales transactions
- Supplies transactions
- Templates
- Payments
- Employees + Salaries
- Auth + Users

**For each module, check:**
- Schema matches decisions in `claude-context.md`
- Behavior matches hard rules in `rules.md`
- Multi-tenancy filter applied
- Audit hooks in place
- No dual sources of truth
- Endpoints respect immutability where required

**Output format per gap:** entity/endpoint → current state → target state → severity → notes. Add to `tech-change-list.md`.

### Phase 2 — Design

Not started. Separate workstream, can run partly in parallel with Phase 3.

**Sub-phases:**
- 2a. Review existing UI screen-by-screen against early-customer workflows
- 2b. Wireframe missing pages: Dashboard, Warehouses, Reports, Notifications, Settings, debt management view

**Output:** `design-context.md` (to be created in Phase 2).

**Tooling:** Claude Design for visuals, Claude Code for implementation. Designs must map to Material UI components.

### Phase 3 — Build

Not started.

**Dependency order:**

1. Multi-tenancy enforcement across all tenant-scoped entities
2. Schema changes for locked decisions: archive flags, audit log table, OriginalTransactionId, AverageCost, WriteOff enum, Transfer entity, InventoryId on TransactionRecord
3. Fix existing feature divergences from Phase 1 audit
4. Warehouses module
5. Inter-warehouse transfers
6. Settings
7. Transaction POS view review and improvements
8. Product details view review and improvements
9. Debt management view
10. Dashboard
11. Reports
12. Notifications
13. Localization pass + Cyrillic↔Latin search
14. Data import (basic CSV for partners and inventory items — needed before first customer onboarding)
15. Mobile read-only finalization

### Phase 4 — Validation

Not started.

Early customers onboard: ice-cream reseller, vitamin importer, furniture reseller. Continuous feedback loop. 4+ weeks usage per customer before public launch.

---

## Open questions

- Multi-tenancy implementation pattern in existing code (Phase 1 audit)
- Data import scope and approach (revisit before first customer onboarding)
- SQL Server backup strategy (before first customer)
- Design system foundations: color palette, typography, MUI customization extent (Phase 2)

---

## Decisions log

- 2026-05-17 — Phase 1 audit completed in Claude Code. Every predicted gap confirmed against current code; module detail and code locations added to `tech-change-list.md`. New "Pre-production hardening" section added for leftover testing shortcuts (debug `Task.Delay`, disabled password verification, disabled SMS). → `tech-change-list.md`, `planning-doc.md`
- 2026-05-17 — Refunds require `OriginalTransactionId`; type must match; refund cannot be refunded; multiple refunds per original allowed but total quantity cannot exceed original. → `rules.md` #2-6, `claude-context.md`
- 2026-05-17 — Audit narrow scope (money/stock events only); single table; EF Core interceptor. → `rules.md` #12-14
- 2026-05-17 — Archive applies to Product and Partner only in MVP. → `rules.md` #15
- 2026-05-17 — Phase 1 audit executed in Claude Code, not chat. → `planning-doc.md`
- 2026-05-17 — Design workstream uses Claude Design, separate context document. → `planning-doc.md`
- Earlier — Payment allocation exposed to user; FIFO shortcut available; advance-payment skip path. → `claude-context.md`
- Earlier — Transactions and payments immutable. → `claude-context.md`, `rules.md`
- Earlier — Partner.Balance computed via DB view, not stored. → `claude-context.md`
- Earlier — InventoryItem is sole source of truth; `Product.QuantityInStock` deprecated. → `claude-context.md`, `rules.md`
- Earlier — Weighted-average cost stored on InventoryItem, atomic update. → `claude-context.md`, `rules.md`
- Earlier — UZS-native, USD captured per event with rate at event time. → `claude-context.md`, `rules.md`
- Earlier — Single-user per business in MVP; multi-user + roles in v2. → `claude-context.md`, `rules.md`
- Earlier — No POS receipt printing in MVP. → `claude-context.md`
- Earlier — Hard-block negative stock in v1. → `claude-context.md`, `rules.md`
- Earlier — No e-faktura / Soliq / Didox / 1C integration. → `claude-context.md`

---

## Next action

Begin Phase 3 (Build), starting with item #1 in the dependency order: **multi-tenancy enforcement across all tenant-scoped entities**. This is the largest blocker and everything downstream (schema changes, feature fixes) depends on the tenant filter pattern being settled first.

Recommended next session: load CLAUDE.md, claude-context.md, rules.md, tech-change-list.md, and plan the multi-tenancy implementation — `OrganizationId` on all 16 tenant-scoped entities, a current-tenant resolution service fed from the JWT, and EF Core global query filters. Confirm the pattern, then extend it.