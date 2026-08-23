# ShoeRetail — Session Handoff (Continuation Point)

> Feed this file to a new Claude Code session opened with working directory = repo root
> (`C:\PROJE`). It has full file-system access — instruct it to read the referenced
> source files rather than relying on paraphrase.

## 0. Source-of-Truth Files (READ THESE FIRST, in this order)

| File | Content | Status |
|---|---|---|
| `docs/00-handoff/ShoeRetail_AI_Agent_Handoff.txt` | Original Phase-0 spec: product vision, business model, full domain rules, roadmap, out-of-scope list. Exhaustive, ~54KB. **Authoritative for anything not repeated below.** | Complete, frozen |
| `docs/database/02-physical-blueprint.md` | Physical DB design log (rationale in Turkish), built table-by-table with user approval each step | 5/16 tables done |
| `database/schema.sql` | Executable DDL mirroring the blueprint, tested against local PostgreSQL | 5/16 tables done, verified working |
| `docs/architecture/folder-structure.md` | Repo layout rationale | Complete |

This handoff file is a compressed index + exact resume point. Do not treat summaries below as replacing the source files — re-read the originals for exact wording/edge cases before making decisions that depend on them.

---

## 1. Session Persona & Working Rules (binding, do not deviate)

- Role: senior architect/mentor. **Preserve existing decisions** — only challenge one if there's a genuinely important technical flaw.
- User is learning while building. For every step: explain theory → why this approach → alternatives rejected (briefly) → exact file/path instructions → runnable code in manageable chunks → how to run/test → expected result.
- **Work incrementally.** Never dump many files/tables at once. Never skip multiple roadmap phases automatically.
- Database design proceeds **table-by-table**; present one table (DDL + rationale), wait for explicit user approval, then proceed.
- User communicates in **Turkish**. Continue responding in Turkish (this handoff doc is in English only because it was explicitly requested that way for portability/token efficiency).
- Avoid fashionable over-engineering: no Repository Pattern over EF Core, no MediatR/CQRS, no microservices, no premature abstraction. Target scale is small (~<1000 orders/month per store). "Simple where possible, robust where necessary" — robustness prioritized for money, stock, auth, audit, backups.
- User's dev machine: Windows 11, PostgreSQL + pgAdmin installed and working locally, dev database named `shoeretail_dev`. Git repo at `C:\PROJE`, pushed to a private GitHub repo. `src/` and `tests/` folders intentionally still empty (Phase 2 not started).

---

## 2. Project Summary

**ShoeRetail** — white-label desktop retail management system for shoe retailers. One codebase → many **independent** installations (own DB per store), NOT multi-tenant SaaS. No customer-specific forks; differences via config/branding/feature-flags only. Two sales models: Standard License (closed source) and Enterprise (full source-code handover) — architecture must support both.

**Stack:** C# / .NET 10 / WPF (MVVM) / ASP.NET Core Web API / EF Core / PostgreSQL / REST+JSON / Git / Windows 11. WPF **never** talks to PostgreSQL directly — always through the API.

**Planned solution structure** (not yet created — Phase 2):
```
ShoeRetail.sln
├── ShoeRetail.Domain          # entities, business rules, no infra knowledge
├── ShoeRetail.Application     # use cases (CreateProduct, ApproveOrder, ReceivePayment...)
├── ShoeRetail.Infrastructure  # EF Core, PostgreSQL, external tech
├── ShoeRetail.Contracts       # API DTOs (privacy boundary — e.g. Customer DTOs exclude PurchasePrice)
├── ShoeRetail.Api             # controllers, auth, validation, middleware
└── ShoeRetail.Desktop         # WPF views/viewmodels/navigation
```

**Roles (V1 — exactly two, do not add more prematurely):**
- **Customer**: browse catalog, search, cart, place order requests, view own orders/status. Cannot see PurchasePrice/margin, cannot touch stock/products, cannot see other customers or financials.
- **Seller**: full management — products, inventory, orders, customers, payments/collections, current accounts, reports, settings.
- **Critical security rule**: PurchasePrice must be absent from the Customer API response payload itself (not just UI-hidden). Backend RBAC enforces this; a Customer hitting a Seller-only endpoint must get 403.

---

## 3. Critical Invariants (NEVER violate, from original handoff)

1. Customer API responses never contain PurchasePrice.
2. Stock can never go negative.
3. Order totals computed server-side only.
4. Client-supplied prices are never trusted — backend re-reads current SalePrice at order-request time.
5. Customer cannot modify Seller-only resources.
6. `OrderItem` stores price/name/SKU/size/color **snapshots** — historical orders never change when Product/Variant data changes later.
7. All financial transactions must remain traceable (ledger, not overwritten fields).
8. Inactive Product/ProductVariant cannot be newly ordered.
9. Invalid order-state transitions are rejected.
10. Passwords never stored in plaintext; PasswordHash never exposed via API or logs.
11. Each retailer installation has its own independent database.
12. Customer order requests require Seller approval before becoming a finalized sale.
13. A Pending order **reserves** stock (does not reduce it).
14. Physical stock (`quantity_on_hand`) decreases only on Seller **approval**.
15. Financial corrections use reversal/correction records, never silent DELETE.

## 4. Hard Delete Policy

`User`, `Customer`, `Product`, `ProductVariant`, `Order`, `OrderItem`, `Payment`, `AccountTransaction`, `AuditLog` → **never hard-deleted**. Use `IsActive`/deactivate/reject/cancel/reverse/correct instead.

## 5. Single-Source-of-Truth Derived Values (do not duplicate as independently stored fields)

- `AvailableQuantity = QuantityOnHand - ReservedQuantity` (DB: `inventory.quantity_available` is a **generated column**, already implemented).
- `Installment.PaidAmount = SUM(active PaymentAllocations)`.
- `Customer.Balance = SUM(AccountTransactions.Amount)`.
- `Order.PaymentStatus` (Unpaid/PartiallyPaid/Paid) — derive from paid vs total, don't store as independently editable.

## 6. Out-of-Scope for V1 (do not build unless explicitly requested)

Mobile app, web customer portal, e-commerce/marketplace integrations, e-invoice/official accounting, bank/online-payment integration, supplier & purchase-order management, multi-branch/multi-warehouse + transfers, advanced returns/refunds workflow, discount/campaign engine, AI features, SMS/WhatsApp/email marketing, offline-first sync.

---

## 7. Roadmap Position

Phases 0–18: Product Definition → Dev Environment → Solution Skeleton → **Database Design (CURRENT)** → Backend Foundation → Auth/RBAC → White-label Config → WPF Foundation → Products → Inventory → Customers → Cart → Orders/Approval → Payments/Installments/Collections → Dashboard/Reporting → Logging/Audit → Testing/Security → Deployment/Backup → Documentation.

**We are inside Phase "Database Design", specifically STEP 2.2 (Final Physical Blueprint).** STEP 2.1 (logical 16-table model) is complete and frozen in the original handoff txt.

---

## 8. STEP 2.2 — Physical Design: Global Conventions (LOCKED, apply to all 16 tables)

| Topic | Decision |
|---|---|
| Primary Key | `bigint GENERATED BY DEFAULT AS IDENTITY` (not UUID — single-DB-per-store model, no distributed sync) |
| Timestamps | `timestamptz`, UTC. Exception: `installments.due_date` uses `date` (no time-of-day meaning) |
| Enums | `varchar` + `CHECK (col IN (...))` — readability prioritized over `smallint` (small scale, source-handover scenario) |
| Money | `numeric(18,2)` everywhere, never float/double |
| FK delete behavior | Default `ON DELETE RESTRICT` (hard-delete already banned; RESTRICT is defense-in-depth). `CASCADE` avoided except rare justified cases |
| Naming | Physical schema `snake_case`; C# side `PascalCase` (EF Core naming convention will bridge this in Phase 4) |
| Recurring pattern | "Type-dependent required field" business rules get a `CHECK` constraint pattern (seen in `users.role`↔`customer_id`, `customers.customer_type`↔`full_name`/`company_name`) — keep applying consistently |
| Recurring pattern | `NOT NULL` does not block empty string `''` — required text fields get an additional `CHECK (btrim(col) <> '')` |
| Recurring pattern | Optional-but-unique-when-present fields (e.g. barcode) use **partial unique indexes**: `CREATE UNIQUE INDEX ... WHERE col IS NOT NULL` |

## 9. STEP 2.2 — Tables Completed (5/16)

`users`, `customers`, `products`, `product_variants`, `inventory` — full DDL in `database/schema.sql` and rationale in `docs/database/02-physical-blueprint.md`. All 5 verified by running the script in pgAdmin against `shoeretail_dev` + manual CHECK-constraint violation tests (all passed as expected).

**Known FK-ordering note:** `users.customer_id → customers(id)`, so physical creation order is `customers` before `users` (opposite of original logical numbering 1,2). This kind of reordering will recur — see next section.

## 10. STEP 2.2 — Tables Remaining (11/16) — target fields per original logical design

Design/discussion will proceed in this order (matches original numbering for continuity); note the **physical execution order in `schema.sql` will differ** where FKs point forward (see dependency-safe order below).

| # | Table | Key fields (see original txt §"CURRENT DATABASE TABLE BLUEPRINT" for full detail) | Special note |
|---|---|---|---|
| 6 | `inventory_movements` | ProductVariantId, OrderId?, MovementType, OnHandDelta, ReservedDelta, Reason?, CreatedByUserId?, CreatedAt | **FK to `orders` — but `orders` doesn't exist yet at this point in discussion order.** Resolve when reached: either design it now without enforcing that FK physically until `orders` exists, or simply place its `CREATE TABLE` later in `schema.sql`. Decide explicitly with user, don't silently drop the FK. |
| 7 | `orders` | OrderNumber (unique, user-facing), CustomerId, RequestedByUserId, ApprovalStatus, FulfillmentStatus, TotalAmount, Notes?, Approved/Rejected metadata | ApprovalStatus{Pending,Approved,Rejected}, FulfillmentStatus{NotStarted,Preparing,Ready,Delivered,Cancelled} — separate enums, not one giant status |
| 8 | `order_items` | OrderId, ProductVariantId, snapshots (ProductName/SKU/Size/Color), UnitSalePrice, UnitPurchasePrice, Quantity | Snapshots are mandatory (invariant #6). `UnitPurchasePrice` is Seller-only, never in Customer DTOs |
| 9 | `payment_plans` | OrderId (**unique**, 1:1), CreatedByUserId, Notes? | |
| 10 | `installments` | PaymentPlanId, SequenceNumber, InstallmentType{DownPayment,Regular}, DueDate, Amount, Notes? | `SUM(installments.amount) = order.total_amount` enforced in Application layer, not DB. Down payment is just a special installment, not separate fields |
| 11 | `payments` | CustomerId, Amount, PaymentDate, PaymentMethod{Cash,BankTransfer,Card,Other}, Reference?, Status{Active,Reversed}, CreatedByUserId, Reversed metadata | Payment ≠ Installment (money received vs money owed) |
| 12 | `payment_allocations` | PaymentId, InstallmentId, Amount | Many-to-many bridge; one payment can cover multiple installments and vice versa |
| 13 | `account_transactions` | CustomerId, OrderId?, PaymentId?, TransactionType, Amount, Description?, CreatedByUserId? | Sign convention: positive = customer owes store. Types: OrderCharge, OrderCancellation, Payment, PaymentReversal, ManualDebit, ManualCredit |
| 14 | `order_history` | OrderId, EventType, OldValue?, NewValue?, Note?, ChangedByUserId, ChangedAt | Business-friendly timeline, separate from generic AuditLogs |
| 15 | `audit_logs` | UserId?, Action, EntityType, EntityId, OldValues? (jsonb candidate), NewValues? (jsonb candidate), Reason? | Never log passwords/hashes/tokens/DB credentials |
| 16 | `store_profile` | StoreName, Phone?, Email?, Address?, CurrencyCode, UpdatedAt | Single-row config table (business info, distinct from deployment/branding config which lives in a config file, not DB) |

**Proposed dependency-safe physical creation order for final `schema.sql`** (confirm/refine with user when compiling the full script):
`customers → users → products → product_variants → inventory → orders → order_items → inventory_movements → payment_plans → installments → payments → payment_allocations → account_transactions → order_history → audit_logs → store_profile`

DB-level CHECK constraints still needed on these (per original spec, not yet written as physical DDL): `order_items.quantity > 0`, `installments.amount > 0`, `payments.amount > 0`, `payment_allocations.amount > 0`, `account_transactions.amount <> 0`.

Enum list still to physicalize (all via `varchar` + `CHECK`, per convention): `ApprovalStatus`, `FulfillmentStatus`, `InventoryMovementType` (InitialStock/Purchase/Sale/Return/ManualIncrease/ManualDecrease/Damaged/OrderReservation/ReservationReleased), `InstallmentType`, `PaymentMethod`, `PaymentRecordStatus`, `AccountTransactionType`, `OrderHistoryEventType`.

Index strategy hints (not finalized) from original spec: `orders(order_number)` unique, `orders(customer_id)`, `orders(approval_status)`, `orders(fulfillment_status)`, `orders(created_at)`; `installments(due_date)` (collections dashboard queries overdue/today/this-week/this-month heavily); `payments(customer_id, payment_date)`; `account_transactions(customer_id, created_at)`; `audit_logs(entity_type, entity_id)`.

---

## 11. Key Business Transactions (must be atomic — implement in Application layer, Phase 4+, but schema must support them)

**Order approval** (Pending→Approved): verify pending + reservation validity → set Approved → `quantity_on_hand -= qty` → `quantity_reserved -= qty` → InventoryMovement → create PaymentPlan + Installments → AccountTransaction `+OrderTotal` → OrderHistory → AuditLog → COMMIT (all-or-nothing).

**Order rejection**: Pending→Rejected → `quantity_reserved -= qty` (physical stock untouched) → InventoryMovement(ReservationReleased) → OrderHistory → AuditLog → COMMIT. No PaymentPlan, no AccountTransaction created.

**Customer order request**: auth+role check → validate product/variant active → check available stock → read current SalePrice server-side → create Order(Pending) + OrderItems (with snapshots) → `quantity_reserved += qty` → InventoryMovement(reservation) → OrderHistory → COMMIT.

**Payment received**: create Payment → auto-allocate to oldest overdue/open installments first → PaymentAllocations → AccountTransaction `-Amount` → AuditLog → COMMIT. (Manual allocation UI is a later, optional advanced action — default flow must stay one-click simple for the seller.)

**Payment reversal**: validate Active → mark Reversed + reason/who/when (record stays in DB) → allocations stop counting toward paid amount → AccountTransaction(PaymentReversal, opposite sign) → AuditLog → COMMIT.

**Concurrency**: order reservation must be safe when two customers race for the last unit — do not solve with client-side checks; use proper DB transaction/locking when implementing in Phase 4+.

---

## 12. Immediate Next Action

Continue **STEP 2.2**, table **6/16 — `inventory_movements`**. Same pattern as tables 1–5: propose full DDL with rationale in Turkish, flag the `orders` FK-ordering issue from §10 explicitly and get the user's decision on it, wait for approval, then append to both `docs/database/02-physical-blueprint.md` and `database/schema.sql`, present both files back to the user.

Do NOT start C# entities / EF Core / WPF until all 16 tables are approved and the user explicitly signs off on the final blueprint (per original handoff's closing instruction).
