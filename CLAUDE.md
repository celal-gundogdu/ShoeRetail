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
  `%APPDATA%\postgresql\pgpass.conf`. Two databases now, both owned by the non-superuser
  `shoeretail` role: **`shoeretail_test`** — ephemeral, owned by the raw-SQL test harness
  (`docs/database/tests/run-tests.ps1` drops and rebuilds it from `schema.sql` on every
  run; never point the app or EF migrations at it, anything written there is designed to
  be thrown away). **`shoeretail_dev`** (created 2026-08-23) — persistent, owned by EF
  Core migrations; this is what the Api's `ConnectionStrings:Default` (in User Secrets)
  points at. Rationale: mixing EF's migration-history table with a database that gets
  dropped out from under it causes silent desync — see blueprint decision log in §12 if
  this needs revisiting.
  Git repo `C:\PROJE`, private GitHub remote.
  **.NET 10 SDK (10.0.400) is installed but NOT first on PATH** — a separate .NET 9
  SDK (9.0.302) lives at `C:\Program Files (x86)\dotnet-sdk-9.0.302-win-x64\` and
  wins PATH resolution, so plain `dotnet` resolves to 9. Use the full path
  `C:\Program Files\dotnet\dotnet.exe` (or Visual Studio, which has its own SDK
  resolution) until PATH is fixed. Fixing it requires admin elevation (move
  `C:\Program Files\dotnet\` ahead of the x86 entry in the **Machine** PATH env var)
  — not done yet, do it in an elevated PowerShell when convenient.
  `src/` and `tests/` now hold the Phase 4 solution skeleton (see §12).

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

Solution structure (created in Phase 4, see §12 for current progress):
```
ShoeRetail.sln
├── src/ShoeRetail.Domain          # entities, business concepts
├── src/ShoeRetail.Application     # ALL business rules / use cases
├── src/ShoeRetail.Infrastructure  # EF Core, PostgreSQL
├── src/ShoeRetail.Contracts       # API DTOs (privacy boundary)
├── src/ShoeRetail.Api             # controllers, auth, validation
├── src/ShoeRetail.Desktop         # WPF
├── tests/ShoeRetail.Domain.Tests
├── tests/ShoeRetail.Api.Tests
└── (Phase 18) Blazor portal hosted inside ShoeRetail.Api
```
Reference graph (enforces §3 rules at compile time — Desktop cannot reach Domain/
Infrastructure even by accident): `Application → Domain` · `Infrastructure →
Application, Domain` · `Api → Application, Infrastructure, Contracts` · `Desktop →
Contracts only`. Test framework: xUnit.

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
FAZ 3   Database Design            ✅ DONE (22 tables, signed off)
FAZ 4   Backend foundation + EF Core + migrations   ✅ DONE
FAZ 5   JWT auth + RBAC            ◀── CURRENT
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

22 tables approved, final blueprint signed off by user 2026-08-23. Phase 4 (C# entities /
EF Core / WPF) is now underway — see §12 for exact progress.

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

**FAZ 3 is COMPLETE.** All 22 tables designed, approved, tested (168 constraint + 11
reconciliation tests, all green — `docs/database/tests/`), and signed off by the user
on 2026-08-23. Re-run the suite after any future change to `schema.sql`:
```
powershell -ExecutionPolicy Bypass -File docs\database\tests\run-tests.ps1
```

**FAZ 4 is underway.** Progress so far:

| Step | Status |
|---|---|
| Solution + 8-project skeleton (`src/`, `tests/`) created, referenced, builds green (0 warnings/errors) | ✅ Done 2026-08-23 |
| .NET 10 SDK installed (see §1 for the PATH caveat — full path needed until fixed) | ✅ Done 2026-08-23 |
| NuGet packages: EF Core 10.0.11 + Npgsql provider 10.0.3 in Infrastructure; `dotnet-ef` as a local tool (`dotnet-tools.json`) | ✅ Done 2026-08-23 |
| `AppDbContext` skeleton (`src/ShoeRetail.Infrastructure/Persistence/`) + `AddInfrastructure()` DI extension + config-driven connection string. Real password lives ONLY in `dotnet user-secrets` (Api project), never in `appsettings.json`. Verified end-to-end via a temporary `/health/db` endpoint against real `shoeretail_test` — got `200 OK`. | ✅ Done 2026-08-23 |
| First entity `StoreProfile` mapped (`EFCore.NamingConventions` for snake_case bridging), migration `InitialCreate_StoreProfile` generated and applied to `shoeretail_dev`, verified with `psql \d` (types/defaults/6 CHECK constraints match `schema.sql` exactly) and a live CHECK-violation test | ✅ Done 2026-08-23 |
| **All 22 entities mapped** (`src/ShoeRetail.Domain/*.cs` + one `IEntityTypeConfiguration<T>` per table in `src/ShoeRetail.Infrastructure/Persistence/Configurations/`), single migration `AddRemainingTables` applied to `shoeretail_dev`. Verified: 73/73 named CHECK constraints match `schema.sql` by name (`diff` clean), FK chain insert (supplier→product→variant→inventory) works, computed column `quantity_available` computes correctly (50−10=40 confirmed live), `chk_users_role_supplier_consistency` correctly rejects a Manufacturer without `supplier_id`. No navigation properties yet — FK scalar ids only (`CustomerId` etc.), deliberate scope cut for speed; add navigation properties in Application layer once real query patterns are known. **Known minor deviation from `schema.sql`:** EF Core's Npgsql provider auto-creates an index on every FK column by convention, so `shoeretail_dev` has a handful of extra indexes beyond what the blueprint explicitly listed (e.g. `ix_supplier_payments_reversed_by_user_id`). Harmless (extra index, not a missing one) but not identical — revisit if it matters later. | ✅ Done 2026-08-24 |
| **`updated_at` auto-refresh decision — RESOLVED: PostgreSQL `BEFORE UPDATE` trigger** (`set_updated_at()` + one trigger per table with `updated_at`, 14 tables). Rationale: per the blueprint's own "DB constraint vs Application rule" test, this is a single-row/single-table rule → belongs in the DB; a trigger also covers writes from outside EF (psql, pgAdmin), which a `SaveChanges` override would miss. Added to `schema.sql` (new section before "22/22 TABLO"), to the blueprint (§"Tekrar Eden Desenler" #6, now marked resolved), and as EF migration `AddUpdatedAtTriggers`. Every `UpdatedAt` property is now `ValueGeneratedOnAddOrUpdate()` so EF reads back the trigger-computed value via `RETURNING` instead of overwriting it. Test coverage: SQL suite test `1.5` (trigger overrides a deliberately wrong value) + golden meta-test `M.2` (every table with `updated_at` has the matching trigger — 169 constraint + 12 golden tests all green) + two new EF Core integration tests in `ShoeRetail.Api.Tests/AppDbContextTests.cs` proving the whole round-trip works through the real ORM against `shoeretail_dev`, not just raw SQL. | ✅ Done 2026-08-24 |
| Navigation properties (Order→Items, Customer→Orders, etc.) — deliberately deferred, add once Application-layer query patterns are known (Faz 7+) | ⬜ (not a Faz 4 blocker) |

**FAZ 4 is COMPLETE.** All items in the roadmap line ("Backend foundation + EF Core + migrations") are done and verified both at the SQL level and through the real EF Core runtime. FAZ 5 (JWT auth + RBAC) is next.

Project reference graph and template choices are recorded in §3.
