# Hard rules

These rules must never be violated. If implementation requires breaking one, stop and raise it.

## Data integrity

1. **Transactions and payments are immutable.** No PUT or DELETE endpoints on TransactionRecord, Payment, PaymentComponent, PaymentAllocation, or Payroll. Corrections only via reverse events.

2. **Refund transactions require `OriginalTransactionId`** (required for SaleRefund and SupplyRefund; null for all other transaction types).

3. **Refund type must match original type.** SaleRefund references Sale only. SupplyRefund references Supply only.

4. **A refund cannot be refunded.** OriginalTransactionId must point to a non-refund transaction.

5. **Sum of refunded quantities per original line item must not exceed the original line quantity.** Enforce at write time across all refunds against the same original transaction.

6. **Multiple refunds against the same original transaction are allowed**, subject to rule 5.

## Multi-tenancy

7. **Every tenant-scoped entity query must filter by OrganizationId.** Tenant-scoped entities: Product, Partner, Category, Inventory, InventoryItem, TransactionRecord, TransactionLine, Payment, PaymentComponent, PaymentAllocation, Template, TemplateItem, Employee, Order, OrderLine, audit log entries.

8. New endpoints must follow the established tenant filter pattern (to be confirmed in Phase 1 audit).

## Inventory

9. **InventoryItem is sole source of truth for stock.** `Product.QuantityInStock` is deprecated. Do not read it. Do not write to it. To be removed.

10. **Weighted-average cost is stored on InventoryItem.** Updated atomically on every stock-in event (Supply, transfer receipt, SaleRefund). Formula: `((existing_qty × existing_cost) + (incoming_qty × incoming_cost)) / (existing_qty + incoming_qty)`.

11. **Hard-block negative stock.** Reject the transaction at write time.

## Audit

12. **Audit only money/stock events.** TransactionRecord, Payment, PaymentComponent, PaymentAllocation, InventoryItem changes, transfer events, opening balance events, opening stock events.

13. **Non-financial edits are not audited** in MVP (product name, partner phone, etc.).

14. **Audit storage:** single table, populated via EF Core interceptor. Capture: actor (user id), timestamp, event type, before-values, after-values, entity type, entity id.

## Archive

15. **Archive (soft-delete via IsDeleted flag) is allowed for Product and Partner only.** No other entities support archive in MVP.

16. **Never hard-delete entities that have referential history.**

## Currency

17. **Partner balances are always stored in UZS internally.**

18. **USD amounts captured per event with the exchange rate at event time.** Never apply "today's rate" to historical events.

## Scope discipline

19. **Do not speculatively design multi-user, roles, or permissions.** Single-user-per-tenant in MVP.

20. **Do not add features beyond `mvp-plan.md` without explicit decision.** Surface scope questions; don't expand silently.