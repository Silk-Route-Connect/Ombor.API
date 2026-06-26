# Backend integrity audit — against current canon

**Date:** 2026-06-26
**Scope:** Read-only. No code changes. Six targeted integrity items checked against `docs/business-rules.md` and `docs/product-brief.md`.
**Method:** Static read of entities, EF configurations, Application services, validators, mappings, contracts, and the seed generators. **No live database was queried** — row-count items (item 3) are answered structurally with the SQL to run against a live DB noted inline.

## Summary

| # | Item | Verdict |
|---|------|---------|
| 1 | `TransactionRecord` warehouse required (non-null)? | ⚠️ **Gap** — nullable in schema; non-null enforced only at the service write path; seed rows are null |
| 2 | `Order` warehouse nullable + Delivered→Sale promotion correct? | ✅ **Conforms** — nullable as intended; promotion confirms warehouse, validates per-line stock, decrements at WAC, atomically. No core bug on this branch |
| 3 | `OriginalTransactionId` non-null enforced for refunds (rule 2)? | ⚠️ **Split** — runtime write path conforms (rules 2–6 enforced); the **seed generator violates it** (null on every seeded refund) |
| 4 | `OrganizationId` scoping via one shared mechanism (rule 34)? | ✅ **Conforms** — single global query filter; Payment/PaymentComponent/PaymentAllocation all scoped; no hand-rolled filters, no bypasses |
| 5 | Org setup seeds wallet+warehouse+category+partner (rule 42)? | ⚠️ **Partial** — all four seeded, but names hardcoded Russian (no language resolution) and partner is `Customer`, not `Both` |
| 6 | Order status history persisted + returned by detail endpoint? | ✅ **Conforms (backend)** — persisted and returned; empty UI is a frontend/contract issue, not backend |

---

## 1. `TransactionRecord` warehouse — nullable

**Canon:** Domain model — "Transaction … has … a warehouse (`InventoryId`)"; every transaction type is a stock event (business-rules.md:140–141: Sale/SupplyRefund = stock-out, Supply/SaleRefund = stock-in). So every transaction moves stock and should always carry a warehouse.

**Current state:**
- Entity: `public int? WarehouseId` — nullable. [TransactionRecord.cs:30](src/Ombor.Domain/Entities/TransactionRecord.cs:30) (the property is named `WarehouseId`, not `InventoryId`; the XML doc says "Required for stock-affecting types").
- EF config makes it **deliberately nullable**: `.HasForeignKey(t => t.WarehouseId).IsRequired(false)` — [TransactionConfiguration.cs:30-35](src/Ombor.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs:30).
- The runtime write path **does** enforce non-null for all four types: `ValidateOrThrowAsync` throws if `!request.WarehouseId.HasValue` and verifies the warehouse exists — [TransactionService.cs:315-323](src/Ombor.Application/Services/TransactionService.cs:315) — then `MoveStockAsync(request.WarehouseId!.Value, …ToStockMovement(), …)` runs for every type — [TransactionService.cs:146-149](src/Ombor.Application/Services/TransactionService.cs:146). `ToStockMovement` maps all of Sale/Supply/SaleRefund/SupplyRefund to a movement — [StockMovementExtensions.cs:30-36](src/Ombor.Application/Extensions/StockMovementExtensions.cs:30).

**Conforms?** Partially. The business invariant is upheld by the only API write path, but the **schema permits a transaction with no warehouse**, and the seed generator actually creates such rows (see below). The invariant rests on a single service-layer guard rather than the data model.

**Gap:** Warehouse should be modeled non-null (`IsRequired(true)`) to match "every transaction moves stock", or the nullability must be justified by a real warehouse-less transaction type (there is none in MVP). As-is it is a defense-in-depth gap, not an active API bug. Note `TransactionGenerator` never sets `WarehouseId` — [TransactionGenerator.cs:16-23](src/Ombor.TestDataGenerator/Generators/TransactionGenerator.cs:16) — so every seeded Sale/Supply/refund in dev/demo/staging has a **null** warehouse and never actually moved stock.

## 2. `Order` warehouse nullable + Delivered→Sale promotion

**Canon:** Domain model (business-rules.md:143) — order write-off warehouse "is **chosen at delivery confirmation** (not at creation)… picks the source warehouse and validates per-line stock against it before promoting. Auto-promotes to a Sale on **Delivered**… if stock is insufficient at promotion, promotion fails per the negative-stock hard block (rule 20)."

**Current state — warehouse nullable (correct):**
- `public int? WarehouseId` with XML doc "Captured at creation and editable while open, but non-binding — it reserves no stock. The authoritative warehouse is chosen at delivery." — [Order.cs:33-34](src/Ombor.Domain/Entities/Order.cs:33). ✅ Correct per canon.

**Current state — `DeliverAsync` promotion** ([OrderService.cs:112-169](src/Ombor.Application/Services/OrderService.cs:112)):
- **(a) Requires a confirmed warehouse:** Yes. `DeliverOrderRequest.WarehouseId` is required by the validator (`GreaterThan(0)`, "A warehouse is required to deliver an order.") — [DeliverOrderRequestValidator.cs:14-16](src/Ombor.Application/Validators/Order/DeliverOrderRequestValidator.cs:14) — and existence-checked before any stock move — [OrderService.cs:124-127](src/Ombor.Application/Services/OrderService.cs:124). The order's creation-time `WarehouseId` is **not** trusted; the delivery request's warehouse is authoritative.
- **(b) Validates per-line stock against it:** Yes. `MoveStockAsync(request.WarehouseId, StockMovement.StockOut, order.Lines…)` — [OrderService.cs:133-136](src/Ombor.Application/Services/OrderService.cs:133). The `StockOut` branch throws `ValidationException` (→ 400) when `item is null || item.Quantity < line.Quantity` — [StockMovementExtensions.cs:69-80](src/Ombor.Application/Extensions/StockMovementExtensions.cs:69). This is the rule-20 hard block.
- **(c) Decrements `WarehouseItem` / writes off at WAC:** Yes. `item.Quantity -= line.Quantity` with `AverageCost` left untouched (stock leaves at WAC) — [StockMovementExtensions.cs:78](src/Ombor.Application/Extensions/StockMovementExtensions.cs:78). The Sale's `TotalDue` is recomputed from the promoted lines, not from the stored order total — [OrderService.cs:138-150](src/Ombor.Application/Services/OrderService.cs:138).
- **Atomicity & state:** The whole promotion is wrapped in an explicit DB transaction (`BeginTransactionAsync` → `CommitAsync`/`RollbackAsync`) — [OrderService.cs:129-166](src/Ombor.Application/Services/OrderService.cs:129). It enforces `Shipping → Delivered` via `ValidateTransition` before touching stock — [OrderService.cs:122](src/Ombor.Application/Services/OrderService.cs:122) — links `order.SaleId`, and appends the `Delivered` history event — [OrderService.cs:154-157](src/Ombor.Application/Services/OrderService.cs:154). `Delivered` is not a valid source for `Delivered` in the transition table, so double-promotion is blocked — [OrderExtensions.cs:9-18](src/Ombor.Application/Extensions/OrderExtensions.cs:9).

**Conforms?** ✅ Yes, on this branch all three sub-checks (a/b/c) are present and correct, and the operation is atomic. The hypothesized "core bug" (missing warehouse confirmation / stock validation / decrement) **does not exist here**.

**Minor notes (not gaps against the audited rule):** the promotion does not re-check `partner.CanHandleTransaction(Sale)` (the customer was validated at order creation); no explicit COGS ledger row is written (consistent with the rest of the system, where COGS is a read-model computation from WAC). Neither breaks canon.

## 3. `OriginalTransactionId` non-null for `SaleRefund`/`SupplyRefund` (rule 2)

**Canon:** Rule 2 — refunds require `OriginalTransactionId` (required for SaleRefund/SupplyRefund, null otherwise). Rules 3–7 add type-match, no-refund-of-refund, quantity caps, and a mandatory reason.

**Current state — entity:** `public int? OriginalTransactionId` (nullable, correct — non-refunds must be null) — [TransactionRecord.cs:34](src/Ombor.Domain/Entities/TransactionRecord.cs:34); EF `IsRequired(false)` — [TransactionConfiguration.cs:37-42](src/Ombor.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs:37).

**Current state — runtime write path (conforms):** Doubly enforced.
- FluentValidation: `OriginalTransactionId NotNull When IsRefund` and `RefundReason NotEmpty When IsRefund` — [CreateTransactionValidator.cs:37-47](src/Ombor.Application/Validators/Transaction/CreateTransactionValidator.cs:37).
- Service: `ValidateRefundOrThrowAsync` re-checks null (rule 2), forbids `OriginalTransactionId` on non-refunds, blocks refund-of-refund (rule 4), enforces type-match (rule 3), and caps cumulative refunded quantity (rules 5–6) — [TransactionService.cs:387-458](src/Ombor.Application/Services/TransactionService.cs:387).

So **no API caller can create a refund with a null `OriginalTransactionId`.**

**Current state — seed path (violates):** The seeders add refund rows **directly**, bypassing the service/validator:
- `AddSaleRefundsAsync` / `AddSupplyRefundsAsync` call `TransactionGenerator.Generate(partner.Id, SaleRefund/SupplyRefund, products, …)` and `context.Transactions.AddRange(...)` — [ProductionDatabaseSeeder.cs:240-294](src/Ombor.TestDataGenerator/Seeders/ProductionDatabaseSeeder.cs:240); same in [DevelopmentDatabaseSeeder.cs:262-313](src/Ombor.TestDataGenerator/Seeders/DevelopmentDatabaseSeeder.cs:262).
- `TransactionGenerator` sets only PartnerId, DateUtc, Type, Status, Lines, TotalDue, TotalPaid — it **never sets `OriginalTransactionId`, `WarehouseId`, or `RefundReason`** — [TransactionGenerator.cs:16-23](src/Ombor.TestDataGenerator/Generators/TransactionGenerator.cs:16).

**Conforms?** Enforcement conforms; **seed data does not.** Any environment seeded by the Development or Production seeder (CLAUDE.md: startup auto-seeds dev) contains refunds with `OriginalTransactionId IS NULL` (and null `RefundReason`, and no stock movement) — violating rules 2 and 7.

**Row count:** Requires a live DB (not run here). Structurally, **every seeded refund** is non-conforming. To get the exact count, run against the target DB:

```sql
SELECT [Type], COUNT(*) AS NullOriginalRefunds
FROM   [TransactionRecord]
WHERE  [Type] IN ('SaleRefund','SupplyRefund')
  AND  [OriginalTransactionId] IS NULL
GROUP BY [Type];
```

(Column/table names per `TransactionConfiguration` — table `TransactionRecord`, enums stored as strings.)

## 4. `OrganizationId` scoping via one shared mechanism (rule 34)

**Canon:** Rule 34 — every org-scoped query filters by `OrganizationId` through the shared mechanism, not a per-endpoint hand-written filter; Payment/PaymentComponent/PaymentAllocation are in the listed scoped entities.

**Current state:**
- **Single shared mechanism:** `OnModelCreating` reflects over the model and applies one global query filter to **every** `IOrganizationScoped` entity — [ApplicationDbContext.cs:57-66, 85-98](src/Ombor.Infrastructure/Persistence/ApplicationDbContext.cs:57). Filter: `e => CurrentOrganizationId == 0 || e.OrganizationId == CurrentOrganizationId`. Insert-time stamping is also centralized in `StampOrganization` — [ApplicationDbContext.cs:100-116](src/Ombor.Infrastructure/Persistence/ApplicationDbContext.cs:100). The contract is documented on the marker interface: "application code must never set or filter on it manually" — [IOrganizationScoped.cs:3-11](src/Ombor.Domain/Common/IOrganizationScoped.cs:3).
- **Payment scoped:** `Payment : … IOrganizationScoped` — [Payment.cs:6](src/Ombor.Domain/Entities/Payment.cs:6).
- **PaymentComponent scoped:** [PaymentComponent.cs:11](src/Ombor.Domain/Entities/PaymentComponent.cs:11).
- **PaymentAllocation scoped:** [PaymentAllocation.cs:6](src/Ombor.Domain/Entities/PaymentAllocation.cs:6).
- **No bypasses:** `IgnoreQueryFilters` appears **nowhere** in `src/`. No hand-written `.Where(x => x.OrganizationId == …)` on any scoped entity. The only manual `OrganizationId` references are: `DashboardService.BusinessNameAsync` reading the accessor to query the **Organization root** (not a scoped entity) — [DashboardService.cs:45-53](src/Ombor.Application/Services/DashboardService.cs:45) — and setup-time **stamping during registration** on User/Organization in `AuthService`/`UserService`/`OrganizationService`, which is the bootstrap path before a JWT exists (mirrors `OrganizationSetupService.SetOrganization`). None are query filters on org-scoped data.

**Conforms?** ✅ Yes. The three payment entities — and all other scoped entities — are filtered by the one shared global filter. No per-endpoint or hand-rolled filter, no `IgnoreQueryFilters` escape hatch.

**Caveat (by design, worth tracking):** `CurrentOrganizationId == 0` short-circuits the filter to "see everything" when there is no org context (seeding / design-time tooling) — [ApplicationDbContext.cs:47-51, 90](src/Ombor.Infrastructure/Persistence/ApplicationDbContext.cs:47). Any code that runs a query while the accessor returns null org id reads cross-org data. Today that is only seeding/setup; if a request ever reaches a query with a null org claim, isolation silently disappears. Not a rule-34 violation, but the one place the guarantee can be lost.

## 5. Org setup seeding (rule 42)

**Canon:** Rule 42 — setup seeds "one Cash wallet, one warehouse, one category, and one partner", all ordinary editable/deletable rows. Domain model & rule 39 frame the starter partner as walk-in retail support (e.g. «Розничный покупатель»). Canon does **not** specify the partner's `PartnerType`, nor a language for the names.

**Current state** ([OrganizationSetupService.cs:11-34](src/Ombor.Application/Services/OrganizationSetupService.cs:11)):
- ✅ Seeds all four: Category «Основная», Partner «Розничный покупатель», Warehouse «Основной склад», Wallet «Касса» (`WalletType.Cash`, opening balance 0).
- ✅ Stamped to the new org via `SetOrganization`; no system flag — ordinary rows (rule 42).
- ⚠️ **Names are hardcoded Russian string literals.** They are **not** resolved from the user's registration language. With per-user language live (M7), a user who registers in Uzbek/English still receives Russian starter data.
- ⚠️ **Partner type is `PartnerType.Customer`**, not `Both` — [OrganizationSetupService.cs:18-23](src/Ombor.Application/Services/OrganizationSetupService.cs:18).

**Conforms?** Against rule 42's literal requirement (four ordinary starter rows) — yes. Against the audit's two specific questions:
- **Language:** Hardcoded Russian, no resolution from registration language. If canon intends localized starter data, this is a gap; canon is currently silent, so this needs a product decision.
- **Partner type Both:** No — it is `Customer`. Canon does not mandate a type. A `Customer` is defensible for a walk-in-retail starter, but it means the starter partner can't be used for Supply transactions out of the box; `Both` would make the single starter record usable for both sales and supplies. **Flagging for confirmation** — this is the item the audit explicitly expected to be `Both`.

## 6. Order status history — persisted and returned

**Canon / contract:** `backend-contract.md` §12 lists order detail as returning `history: OrderStatusEvent[]` and the `GET /api/orders/{id}` row as "full detail + history" — [backend-contract.md:403,417](docs/backend-contract.md:403).

**Current state:**
- **Persisted:** `OrderStatusEvent` is an entity with its own table; a row is appended at creation (`From = null`) — [OrderService.cs:25-32](src/Ombor.Application/Services/OrderService.cs:25) — on every status transition — [OrderService.cs:205-213](src/Ombor.Application/Services/OrderService.cs:205) — and on delivery — [OrderService.cs:157](src/Ombor.Application/Services/OrderService.cs:157). Entity: [OrderStatusEvent.cs](src/Ombor.Domain/Entities/OrderStatusEvent.cs).
- **Returned:** Every read projects history. `ProjectAsync` does `.Include(x => x.History)` and resolves actor display names — [OrderService.cs:259-298](src/Ombor.Application/Services/OrderService.cs:259) — and `GetByIdAsync` (the detail endpoint) routes through it — [OrderService.cs:47-52](src/Ombor.Application/Services/OrderService.cs:47). The mapping populates `OrderDto.History` (sorted oldest-first, with `From/To/At/By`) — [OrderMappings.cs:66-101](src/Ombor.Application/Mappings/OrderMappings.cs:66). The contract field exists: `OrderDto.History` is `OrderStatusEventDto[]` — [OrderDto.cs:42, 72-76](src/Ombor.Contracts/Responses/Order/OrderDto.cs:42).

**Conforms?** ✅ Backend conforms — history is persisted and returned by the detail (and list) endpoints.

**Gap (not backend):** If the UI renders history empty, the cause is on the frontend/contract side — e.g. the client reading a different field name or not yet wired to `OrderDto.History`. One detail to verify against the frontend: the DTO sorts history **oldest-first** (`OrderBy(e.At)`), while the contract comment says "newest-first" — [OrderMappings.cs:93-95](src/Ombor.Application/Mappings/OrderMappings.cs:93) vs [OrderDto.cs:22](src/Ombor.Contracts/Responses/Order/OrderDto.cs:22). That is an ordering mismatch, not the cause of an empty list. Recommend confirming the frontend binding in `docs/frontend-fixes.md`.

---

## Cross-cutting finding: the seed generators bypass invariants

Items 1 and 3 share one root cause: `TransactionGenerator` ([TransactionGenerator.cs](src/Ombor.TestDataGenerator/Generators/TransactionGenerator.cs)) builds `TransactionRecord`s that are inserted directly via `context.Transactions.AddRange`, never through `TransactionService`. The generated rows omit `WarehouseId` (item 1), `OriginalTransactionId` and `RefundReason` for refunds (item 3, rules 2 & 7), and never run `MoveStockAsync`, so seeded refunds/sales carry no corresponding stock movement. Any dev/demo/staging DB seeded this way contains data the API itself would reject. This is a **seed-data integrity** issue, distinct from the runtime enforcement (which conforms for items 3’s rules). Worth fixing before the data is used to demonstrate the dispute-grade audit trail.

## Open questions (for confirmation)

1. **Item 1:** Should `TransactionRecord.WarehouseId` be made non-null in the schema to match "every transaction moves stock", or is a warehouse-less transaction type intended? (No such type exists in MVP today.)
2. **Item 5 — partner type:** Should the starter partner be `Both` (usable for sales *and* supplies) rather than the current `Customer`? Canon is silent; the audit expected `Both`.
3. **Item 5 — language:** Should starter-record names be localized to the registering user's language, or is Russian-only acceptable for MVP? Canon does not specify.
4. **Item 3 — seed data:** Confirm whether the Development/Production seeders are expected to produce canon-valid refunds (with `OriginalTransactionId`/`RefundReason`/stock movement), or whether seed data is explicitly exempt.
