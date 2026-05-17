# Ombor

Warehouse management system for small businesses in Uzbekistan. Replaces paper and Excel. Trilingual (UZ-Latin, UZ-Cyrillic, RU). Web-first with read-only mobile companion.

## Tech stack

- Backend: .NET + SQL Server (EF Core)
- Web: React + TypeScript + Material UI
- Mobile: React Native (read-only in v1)
- Hosting: Hetzner; nginx serves static frontend

## Documentation map

Load these before non-trivial work:

- `claude-context.md` — business + technical context + locked decisions
- `rules.md` — hard rules that must never be violated
- `mvp-plan.md` — feature scope and acceptance criteria
- `tech-change-list.md` — known gaps, target state, severity
- `planning-doc.md` — current phase, next steps, live status
- `project-context.md` — human-readable onboarding for team members

## Critical rules summary

Full list in `rules.md`. Highlights:

1. Transactions and payments are immutable. No PUT/DELETE.
2. Every tenant-scoped entity query filters by TenantId (enforced via EF Core global query filter).
3. InventoryItem is sole source of truth for stock. `Product.QuantityInStock` is deprecated; do not read or write it.
4. Refund transactions require `OriginalTransactionId`. Type must match: SaleRefund→Sale, SupplyRefund→Supply.
5. Audit only money/stock events. Single audit table, EF Core interceptor.
6. Archive (soft-delete) for Product and Partner only. Never hard-delete.
7. Don't speculatively design multi-user, roles, or permissions.

## How to work

- Decisions land in the appropriate doc before moving on.
- When scope is ambiguous, surface it; don't expand silently.
- Err toward simplicity, audit integrity, computed over stored, defer to v2.
- Coding conventions and additional rules added as patterns emerge.