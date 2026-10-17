# Changelog

What changed and why, newest first. Git history has the line-by-line detail; this file is the readable version.

## 2026-10-15 — Ported the missing features from the original API; moved to PostgreSQL

The original single-project API (`nourokbi/audiophile`) had auth, addresses and product admin that this rebuild didn't.
They were rebuilt here for the four layers and ASP.NET Core Identity, not copied line by line.

### Ported from the original
- **Auth:** register, login, `me`, refresh, logout with JWT (`AuthService`, `AuthController`).
- **Addresses:** list, get, create, update, soft delete (`AddressService`, `AddressesController`).
- **Products:** list with search, category and price filters; get by id; admin create / update / deactivate
  (`ProductService`, `ProductsController`).
- **PostgreSQL:** Npgsql provider, EF migrations, `jsonb` for "in the box", check constraints, unique indexes,
  delete behaviour per relationship.
- **Rate limiting** on login, refresh and register.

### Added (not in the original)
- Refresh tokens **rotate** on every refresh, are **stored as SHA-256 hashes**, and **reuse revokes every session**
  of that user (build plan M2-01, M2-03).
- **Lockout**: 5 wrong passwords lock the account for 5 minutes.
- **Admin account from configuration** at startup (`DatabaseInitializer`); `Admin` and `Customer` **roles seeded** by
  the migration (M1-12).
- **Paging** on the product list (M7-01).
- **CORS** from `Cors:AllowedOrigins`, and **forwarded headers** so the rate limiter sees the real client IP behind a
  hosting proxy (M1-10, M1-11).
- **More check constraints:** cart line quantity 1–100, discount ≤ subtotal, refunded ≤ amount, prices > 0.
- `ICurrentUser` (the signed-in user's id from the token) and `ITokenService` (issues tokens) abstractions.
- `Product.CorrectStock(count)`, so an admin can set a stock count; `Product._inTheBox` is no longer `readonly`,
  so EF can load the `jsonb` column into it.
- `docker-compose.yml` (PostgreSQL 17) as one way to run the database locally; `docs/database.md`, `docs/api.md`.
- **8 HTTP integration tests** (`tests/Audiophile.Tests/Api`) that run the real API in memory. 33 tests in total.

### Changed compared with the original
- Register returns **201**, not 200 (M2-06).
- Logout **requires login** and only revokes the caller's own token (M2-04).
- Deleting a missing address returns **404**, not 204.
- Rate limits: login/refresh **10 per minute** (was 5), register **5 per hour** (was 3).
- Product responses include the **stock count** as well as `inStock`.
- Table names are plural everywhere (`CartItems`, `OrderItems`).
- Roles come from Identity's tables, not a `UserRole` enum (D-02); passwords are hashed by Identity, not BCrypt (D-01).

### Removed
- `DemoUser`: every request now acts as the signed-in user.
- The `wwwroot` demo storefront: it had no login. The React client replaces it (Phase 4).
- SQLite from the app. Only the unit/integration tests still use it, until Testcontainers (Phase 1).

### Verified
- First run on the local PostgreSQL 18: `InitialCreate` applied, admin account created, and a 22-step smoke test
  passed (register, login, `me`, cart, addresses, cash-on-delivery order + idempotency, refresh rotation and reuse
  detection, 403 for customers, admin product create with `jsonb`, deactivate).

### Not ported
- `Coupon`: deferred by the build plan.
- The `xmin` stock concurrency token: it needs PostgreSQL in the tests too, so it comes with the concurrency test in Phase 1.

## 2026-10-14 — Revision, plan and clean-up

- Wrote `project-overview.md`, `use-cases.md`, `diagrams.md` and `roadmap.md`; marked `build-plan.md` as superseded.
- Deleted the old generated HTML docs and the old phase task lists.
- Split `EntityConfigurations.cs` into one file per entity under `Persistence/Configurations/`.
- Replaced `DbSeeder` with `HasData` seed data inside the configurations.
- Removed the empty-folder `.gitkeep` files.
- Cart API (`CartService`, `CartController`, 7 tests); payment gateway code switched off with
  `// PAYMENT GATEWAY (off)` markers while checkout is rebuilt.
