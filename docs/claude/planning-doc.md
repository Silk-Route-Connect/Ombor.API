# Ombor planning

Live document. Updated as phases progress. Master "where are we" reference.

---

## Current phase

**Phase 3 — Build, in progress.** Phase 1 audit complete; gaps recorded in `tech-change-list.md`. Build item #1 (multi-tenancy enforcement) landed 2026-05-17. Next: build item #2 (schema changes for locked decisions).

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

In progress.

**Dependency order:**

1. ~~Multi-tenancy enforcement across all tenant-scoped entities~~ — **done 2026-05-17.** `Organization` renamed to `Tenant`; `ITenantScoped` + `TenantId` on every tenant-scoped entity; EF Core global query filters + insert stamping; `ITenantAccessor` reads the `tenant_id` JWT claim. Migration `Add_Multi_Tenancy`.
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

- 2026-05-17 — Multi-tenancy concept renamed `Organization` → `Tenant` end-to-end (entity, service, DbSet, FK `OrganizationId` → `TenantId`, contracts). Tenant scoping enforced via `ITenantScoped` + EF Core global query filters; `TenantId` stamped on insert; tenant resolved from a `tenant_id` JWT claim by `ITenantAccessor`. Phase 3 build item #1 complete. → `rules.md` #7-8, `claude-context.md`, `tech-change-list.md`
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

Phase 3 item #1 (multi-tenancy) is done. Next is **item #2 — schema changes for locked decisions**: archive flags (`IsDeleted` on Product/Partner), audit log table + EF Core interceptor, `OriginalTransactionId` on TransactionRecord, `AverageCost` on InventoryItem, `WriteOff` enum value, Transfer entity, `InventoryId` on TransactionRecord.

Note for the next session: integration tests could not be executed in the multi-tenancy session because Docker (Testcontainers/SQL Server) was unavailable — the integration project compiles but the suite still needs a run on a Docker-capable machine. Unit tests (288) pass.