# ShoeRetail — Session Bootstrap

> Read this first in every new session. It is the compressed index + exact resume point.
> Written in English for token efficiency; **the user communicates in Turkish and you
> must respond in Turkish.**

## 0. Source-of-Truth Files (read in this order)

| File | Content |
|---|---|
| `docs/00-handoff/02-project-spec-v2.md` | **Authoritative** full business/domain spec (post-pivot). Turkish. |
| `docs/database/02-physical-blueprint.md` | Physical DB design log, table-by-table with rationale. Turkish. |
| `docs/database/schema.sql` | Executable DDL mirroring the blueprint. |
| `docs/architecture/folder-structure.md` | Repo layout rationale. |
| `docs/00-handoff/archive/README.md` | What the pivot changed and why. Read if a decision looks contradictory. |

⚠️ `docs/00-handoff/archive/` and `docs/database/archive/` contain **obsolete pre-pivot**
documents. Never treat them as current. They exist for historical traceability only.

Do not treat this file's summaries as replacing the sources — re-read the originals
before making decisions that depend on exact wording.

---

## 1. Working Rules (binding, do not deviate)

- Role: senior architect / mentor. **Preserve existing decisions**; challenge one only
  if there is a genuinely important technical flaw.
- User is learning while building. Every step: theory → why this approach →
  alternatives rejected (briefly) → exact file path → runnable code in manageable
  chunks → how to run/test → expected result.
- **Work incrementally.** Never dump many files/tables at once. Never skip multiple
  roadmap phases automatically.
- Database design proceeds **table-by-table**: present one table (DDL + rationale),
  wait for explicit approval, then write to both `02-physical-blueprint.md` and
  `schema.sql`.
- Respond in **Turkish**.
- No fashionable over-engineering: no Repository Pattern over EF Core, no MediatR/CQRS,
  no microservices, no premature abstraction. Scale is small.
  **"Simple where possible, robust where necessary"** — robustness prioritized for
  money, stock, auth, audit, backups.
- Dev machine: Windows 11, PostgreSQL 18 + pgAdmin local. **Port 5433, not 5432.**
  `psql` is at `C:\Program Files\PostgreSQL\18\bin\` (not on PATH); credentials live in
  `%APPDATA%\postgresql\pgpass.conf`. Databases: `shoeretail_test` (owner: the
  non-superuser `shoeretail` role — what the app connects as). There is no
  `shoeretail_dev`; earlier versions of this file wrongly claimed there was.
  Git repo `C:\PROJE`, private GitHub remote.
  `src/` and `tests/` intentionally empty (Phase 4 not started).

---

## 2. Business Model (post-pivot — this changed, read carefully)

We are a **shoe wholesaler / distributor**.

```
MANUFACTURERS ──▶ US (wholesaler) ──▶ RETAIL SHOPS
 use the app        use the app        do NOT use the app
 (Blazor portal)    (WPF desktop)      (phone orders, we key them in manually)
```

The original design assumed the opposite (retailers self-serve, manufacturer absent).
That assumption is dead — see `docs/00-handoff/archive/README.md`.

**Consequence:** fast, easy manual data entry is the product's primary competitive
advantage. Every design decision must be checked against "does this make order entry
easier?"

The app is the business's flagship system: stock, both-sided orders, both-sided
receivables/payables, reports — all in one place.

---

## 3. Stack

C# / .NET 10 · WPF+MVVM (Owner console) · Blazor Server (manufacturer portal, Phase 18)
· ASP.NET Core Web API · EF Core · PostgreSQL · REST+JSON · **JWT auth** · Windows 11

Planned solution structure (Phase 4, not created yet):
```
ShoeRetail.sln
├── ShoeRetail.Domain          # entities, business concepts
├── ShoeRetail.Application     # ALL business rules / use cases
├── ShoeRetail.Infrastructure  # EF Core, PostgreSQL
├── ShoeRetail.Contracts       # API DTOs (privacy boundary)
├── ShoeRetail.Api             # controllers, auth, validation
├── ShoeRetail.Desktop         # WPF
└── (Phase 18) Blazor portal hosted inside ShoeRetail.Api
```

### 🔒 Immutable architecture rules (prerequisites for cheap Blazor addition later)
1. Business logic never lives in UI. ViewModels only display and call the API.
2. WPF has zero privileges — no direct DB/file access, same API as everyone.
3. Auth is JWT, never Windows/session-based.
4. Nothing hardcoded: API address, DB connection, stock-code prefix all come from
   config or DB. VPS migration must require no code change.
5. HTTPS from day one, even on localhost.

---

## 4. Roles (exactly two in V1)

- **`Owner`** — us. Full access.
- **`Manufacturer`** — external supplier. Sees ONLY their own purchase orders.

### 🔴 #1 Privacy Rule (this REPLACED the old "Customer never sees PurchasePrice")
**The manufacturer must never see our sale prices, retailer customer list, sales
orders, revenue, or margins.** They are a supplier and a potential competitor; leaking
the customer list would let them bypass us entirely.

Enforced in three layers: (1) separate tables for retailer vs supplier finance —
structural isolation; (2) manufacturer DTOs simply have no `sale_price` field;
(3) backend RBAC → 403, plus every query filtered by the caller's `supplier_id`.

`Staff` role is deliberately not built yet, but `users.role` must make adding it a
one-line change.

---

## 5. Key Invariants

1. 🔴 Manufacturer responses contain no sale price, customer data, or margin.
2. 🔴 A manufacturer cannot see another manufacturer's orders.
3. Stock can never go negative.
4. Order totals computed server-side only; client prices never trusted.
5. `order_items` store name/code/SKU/size/color/price **snapshots**.
6. All financial movement is traceable (ledger, never an overwritten field).
7. Inactive product/variant cannot be newly ordered.
8. Invalid order-state transitions rejected.
9. Passwords never plaintext; `password_hash` never in API or logs.
10. Each installation has its own independent database.
11. **Order receipt reserves stock; physical stock decreases ONLY on shipment.**
12. Financial corrections use reversal records, never silent DELETE.
13. Stock-code prefix comes from config, never hardcoded.

**Hard delete banned** on: `users`, `customers`, `suppliers`, `products`,
`product_variants`, `orders`, `order_items`, `purchase_orders`,
`purchase_order_items`, `payments`, `supplier_payments`, `account_transactions`,
`supplier_transactions`, `audit_logs`. Use `is_active` / cancel / reverse instead.

**Derived, never stored independently:** `quantity_available`, installment paid amount,
customer balance, supplier balance, order payment status, purchase-order fulfillment
status.

---

## 6. Stock Code

Format `GND000142` = 3-letter prefix + 6 zero-padded digits.
Lives on **`products`** (identifies a model, not a variant).
Variant SKU auto-generated: `GND000142-41-SYH`.

**White-label critical:** the `GND` prefix is NOT hardcoded. DB `CHECK` enforces only
the general shape; `store_profile.stock_code_prefix` / `.stock_code_digits` hold the
actual values; the Application layer generates and validates codes.

UI: user types `142` → app expands to `GND000142`. New products get the next free code
suggested automatically.

---

## 7. Order Lifecycle (approval step was REMOVED)

```
Received  → stock RESERVED (physical unchanged)
Preparing
Shipped   → ★ physical stock decreases, retailer is charged, payment plan created
Delivered
Cancelled (any stage) → reservation released
```

---

## 8. Deployment — two stages

- **Stage 1 (now):** everything on the owner's single PC. Manufacturer portal off.
- **Stage 2 (when budget allows):** API + PostgreSQL on a VPS; WPF connects remotely;
  Blazor manufacturer portal goes live.

Backups must never live only on the same physical disk as the database.
A backup strategy is incomplete until a restore has been tested.

---

## 9. Roadmap

```
FAZ 3   Database Design            ◀── CURRENT (22 tables)
FAZ 4   Backend foundation + EF Core + migrations
FAZ 5   JWT auth + RBAC
FAZ 6   WPF shell / MVVM / navigation
FAZ 7   Products + stock code + variants
FAZ 8   Inventory + movements
FAZ 9   Retail customers
FAZ 10  ★ Sales order entry (size-distribution grid — the flagship screen)
FAZ 11  Shipment + stock decrement
FAZ 12  Suppliers + purchase orders
FAZ 13  Goods receipt
FAZ 14  Retailer finance (plans / installments / collections / ledger)
FAZ 15  Supplier finance (payables / payments / ledger)
FAZ 16  Dashboard + reports
FAZ 17  Logging / audit / error handling
FAZ 18  ★ VPS migration + Blazor manufacturer portal
FAZ 19  Testing / security / performance
FAZ 20  Backup / installer / documentation / handover
```

Do NOT start C# entities / EF Core / WPF until all 22 tables are approved and the user
explicitly signs off on the final blueprint.

---

## 10. STEP 2.2 — Physical Design Conventions (LOCKED, all tables)

| Topic | Decision |
|---|---|
| Primary key | `bigint GENERATED BY DEFAULT AS IDENTITY` (not UUID — single DB per install) |
| Timestamps | `timestamptz`, UTC. Exception: date-only business dates use `date` |
| Enums | `varchar` + `CHECK (col IN (...))` — readability over `smallint` |
| Money | `numeric(18,2)`, never float/double |
| Quantities | `integer` |
| FK delete behavior | Default `ON DELETE RESTRICT` (defense in depth; hard delete already banned) |
| Naming | Physical `snake_case`; C# `PascalCase` (EF Core naming convention bridges) |
| Type-dependent required fields | `CHECK` constraint pattern (e.g. `users.role` ↔ `supplier_id`) |
| Required text | `NOT NULL` does not block `''` → add `CHECK (btrim(col) <> '')` |
| Optional-but-unique | Partial unique index: `CREATE UNIQUE INDEX ... WHERE col IS NOT NULL` |
| FK columns | PostgreSQL does not auto-index them — add the index manually |
| `updated_at` | Not auto-updated yet; trigger vs EF `SaveChanges` override decided in Phase 4 |

---

## 11. The 22 Tables & Dependency-Safe Creation Order

| # | Table | Status |
|---|---|---|
| 1 | `store_profile` | ✅ |
| 2 | `customers` | ✅ (v1 design valid, re-confirm) |
| 3 | `suppliers` | ✅ NEW |
| 4 | `users` | ✅ CHANGED (role/supplier_id) |
| 5 | `products` | ✅ CHANGED (+ stock_code) |
| 6 | `product_variants` | ✅ |
| 7 | `inventory` | ✅ CHANGED (+ low_stock_threshold) |
| 8 | `orders` | ✅ CHANGED (no approval_status) |
| 9 | `order_items` | ✅ |
| 10 | `order_history` | ✅ |
| 11 | `purchase_orders` | ✅ NEW |
| 12 | `purchase_order_items` | ✅ NEW |
| 13 | `purchase_order_history` | ✅ NEW |
| 14 | `inventory_movements` | ✅ (now placed after both order tables — FK ordering resolved) |
| 15 | `payment_plans` | ✅ |
| 16 | `installments` | ✅ |
| 17 | `payments` | ✅ |
| 18 | `payment_allocations` | ✅ |
| 19 | `account_transactions` | ✅ |
| 20 | `supplier_payments` | ✅ NEW |
| 21 | `supplier_transactions` | ✅ NEW |
| 22 | `audit_logs` | ✅ |

Enums to physicalize (all `varchar` + `CHECK`): `UserRole`, `CustomerType`,
`OrderStatus`, `PurchaseOrderStatus`, `InventoryMovementType`, `InstallmentType`,
`PaymentMethod`, `PaymentRecordStatus`, `AccountTransactionType`,
`SupplierTransactionType`, `OrderHistoryEventType`.

Index hints from the spec: `products(stock_code)` unique; `orders(order_number)` unique,
`orders(customer_id)`, `orders(status)`, `orders(created_at)`;
`purchase_orders(supplier_id)`, `purchase_orders(status)`,
`purchase_orders(payment_due_date)`; `installments(due_date)` (collections dashboard);
`payments(customer_id, payment_date)`; `account_transactions(customer_id, created_at)`;
`supplier_transactions(supplier_id, created_at)`; `audit_logs(entity_type, entity_id)`.

---

## 12. Immediate Next Action

**STEP 2.2 is COMPLETE — all 22 tables are designed, approved, and written** into
`docs/database/02-physical-blueprint.md` and `docs/database/schema.sql`.

✅ **The schema is tested and green.** `docs/database/tests/` holds a runnable suite:

```
powershell -ExecutionPolicy Bypass -File docs\database\tests\run-tests.ps1
```

It drops and rebuilds `shoeretail_test` from `schema.sql`, seeds it, then runs
168 constraint tests + 11 reconciliation ("golden") tests. All pass; 79% of the 129
constraints/unique indexes are exercised (the rest are FKs, sampled by family). Read
`docs/database/tests/README.md` before touching it — it documents the test harness and
four traps that already cost time (psql dollar-quote nesting, identity sequences vs
explicit ids, non-idempotent reset, PowerShell 5.1 native stderr).

**Re-run the suite after ANY change to `schema.sql`.**

Bug the first run caught (now fixed, with regression tests): four CHECK constraints used
`btrim(col) <> ''` on a nullable column with no `IS NOT NULL` guard. `btrim(NULL) <> ''`
is `NULL`, and PostgreSQL treats a `NULL` CHECK result as satisfied — so rows with no
justification were silently accepted. See blueprint "Tekrar Eden Desenler" #5; golden
test M.1 guards against a repeat.

Remaining before Phase 4: user signs off on the final blueprint. Only then start
C# entities / EF Core / WPF.
