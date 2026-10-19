# Changelog

What changed and why, newest first. Git history has the line-by-line detail; this file is the readable version.

## 2026-10-19 — Admin product actions

- Every row of the admin product table has *Restock*, *Price* and an *Available* switch, in a small dialog with a
  preview of the change. The calls live in `client/src/lib/adminApi.js` and only change the page until the admin API
  exists; the roadmap lists the endpoints to build.
- React Query and Chart.js are planned for right after the admin API (roadmap, Phase 5).

## 2026-10-19 — Cart page, address picker, pagination

- Cart items are bigger cards with the photo, category, description, stock and a link to the product page.
- *Export CSV* for recent orders on the admin dashboard.
- The product counter shows the range on the page, like *Showing 10 to 18 of 22 products*.
- Log out in the user drawer is red.

- **Cart page** at `/cart`, opened from the cart icon, instead of the drop-down: quantities, remove, totals, how
  much more for free shipping, and the way to checkout.
- **Checkout address picker**: choose one of your saved addresses or *Add a new address*; with none saved the form
  shows straight away. A new address is saved once, so retrying after an error does not save it twice.
- **Pagination** on the storefront: nine products per page with previous, page numbers and next.
- **Order tracker**: the connecting line now runs under the step circles instead of over them.

## 2026-10-19 — Dark theme, user drawer, filters, new collection, admin dashboard

### Added
- **16 new products** in five categories (two new ones: `Amplifiers` and `Accessories`), seeded by migration
  `SeedNewCollection`, with photos from the Stitch project in `client/public/assets/products`.
- **Dark theme**: every color is a CSS variable with a light and a dark value, switched from the user drawer or
  Settings and remembered in the browser.
- **User drawer** from the account button: My orders, Admin dashboard (admins only), Settings, theme switch, Log out.
- **Header**: Home, Categories (opens on hover) and New collection in the middle; `/new` lists the new collection.
- **Filters**: price range and sort, plus *Clear filters*.
- **Settings page**: profile, theme, and saved addresses with delete.
- **Admin dashboard** (`/admin`, admins only): live products and stock, sample revenue and orders until the admin API.

### Changed
- The API is now the only source of product text and prices; `data.json` became `pictures.json` with just the
  photos and galleries (570 lines to 209). "You may also like" picks products from the same category.

## 2026-10-19 — The shop gets the Stitch design

### Changed (client)
- Every customer page rebuilt from the light screens of the Stitch project: header, storefront, product page,
  login and register, checkout, my orders, order page, footer. Tailwind with ten named colors and a few shared
  classes (`card`, `btn-primary`, `input`, …) in `index.css`; icons from `lucide-react`.
- `CatalogContext` hands out finished products (text and pictures from `data.json`, id, price and stock from the
  API), so no page joins the two any more.
- `/headphones`, `/speakers` and `/earphones` are the storefront with that category selected; `Category`, `Menu`
  and `BestGear` were removed.
- New on the storefront: search and an *in stock only* switch. New on My orders: status tabs, an order tracker and
  an account summary. Show password button on login and register.
- `Picture` replaces the three desktop, tablet and mobile `<img>` tags, and the signed in routes share one
  `RequireAuth` parent route.
- `npm run lint` passes with zero warnings. `docs/frontend.md` describes the structure and the design tokens.

### Changed (API, development only)
- The admin account comes from `Admin:Email` and `Admin:Password` in user-secrets and its password is set straight
  from there on every start, so a short development password works.
- `Database:MigrateOnStartup` is off in Development: apply new migrations with `dotnet ef database update`.

## 2026-10-18 — Phase 1 done (`v0.2.0`)

### Added
- **Retry on a stock conflict**: `OrderService.RetryOnStockConflictAsync` runs checkout (and cancel) once more on
  `DbUpdateConcurrencyException`; a second conflict is a 409.
- **`POST /orders/{id}/cancel`** for the order's owner, while the status allows it. A paid card order is a 409
  "contact support", since refunds are done in the Fawaterak dashboard. *Cancel order* button on the order page.
- Tests: `OrderApiTests` (checkout, idempotency, cancel, someone else's order is a 404, two checkouts for the
  last unit) and `StockConcurrencyTests` (`xmin` refuses the second write). 37 tests.
- **GitHub Actions** (`.github/workflows/ci.yml`): build and test the API against PostgreSQL, build the shop.
- `docs/checkout-flow.md`: one order followed through the code.

### Changed
- Development tokens last longer (access token 7 days, refresh token 30 days, in `appsettings.Development.json`),
  so you stay signed in while working. Production keeps 15 minutes and 14 days.

## 2026-10-18 — Simpler code

### Changed
- `OrderService` only places and reads orders. Everything about the card payment (open the Fawaterak page,
  sync, expiry, payment events) moved to a new `PaymentService`.
- `Order.Place` takes the address and the cart's products directly: it copies the address, reserves the
  stock, works out the shipping fee and creates the payment. `Order.Cancel` returns the stock and fails a
  pending payment. `OrderLine`, `ShippingDetails` and `OrderStock` are gone.
- `IFawaterakGateway` works with the `Order` itself: `CreateInvoiceAsync`, `GetInvoiceAsync` and
  `ReadPaidInvoiceAsync` (a forged webhook throws, so it answers 401). Amounts stay in pounds, with no
  piaster round trip.
- A paid invoice for an order that was already cancelled is now reported as `NeedsManualRefund`
  (before, it was silently `Ignored`).
- Every code comment is one plain line, without FLOW, step or requirement labels.

### Removed
- Refund, void and discount code nobody used (Fawaterak has no refund API): `Payment.RecordRefund`,
  `MarkVoided`, the `PartiallyRefunded`/`Refunded`/`Voided` statuses, and the `RefundedAmount` and
  `DiscountAmount` columns (migration `RemoveDiscountAndRefundColumns`).
- Unused domain methods (`User.UpdateName`, `UpdateEmail`, `SetPhone`, `RemoveAddress`, …), the
  form-encoded webhook path, and the plain `/payments/fawaterak/webhook` route (invoices always use `webhook_json`).

## 2026-10-18 — The shop talks to the API (login → cart → pay → confirmed)

### Added (client)
- `lib/api.js` (fetch wrapper with one automatic token refresh), `AuthContext`, `CartContext` (the server
  cart), `CatalogContext` (the API's products by slug).
- **Login** and **Register** pages in the shop's design; `RequireAuth` sends anonymous visitors to log in
  and back.
- The cart drop-down, product page and checkout use the API: prices and stock are the API's, the checkout
  saves or reuses the address, places the order with an `Idempotency-Key`, and sends Card payments to
  Fawaterak's page.
- **My orders** and an **order progress page** (`/orders/:id`) that polls until the webhook confirms the payment.
- Vite also forwards `/payments` to the API, so `ngrok http 5173` serves the shop and receives the webhook.
- **Home page is the catalogue**: after the hero, every product is a card (picture, price and stock from the
  API, a quantity stepper and *Add to cart*), filtered by category chips kept in the URL (`/?category=speakers`).
  The picture and name open the product page. The promo blocks and category tiles were removed from the home page.
- `useAddToCart` and `QuantityStepper` are shared by the cards and the product page.

### Changed (API)
- `Product.Slug` (unique; migration `SeedStorefrontProducts`). The seed is now the storefront's six products
  with their real text, features, box contents, cart images and prices (EGP); the four existing ids are kept.
- `OrderItemDto` includes `productImageUrl`.

### Verified
- Through the Vite proxy and through ngrok: products, sign up, cart, card order with a real Fawaterak invoice,
  return redirect (302 to `/orders`), forged webhook 401. Screenshots of login, product, checkout and order pages.
- Not verified here: paying in the browser and the real webhook. That needs a person at the payment page.

## 2026-10-18 — Checkout from the cart; Fawaterak back on

### Removed to keep the payment flow small
- The `VodafoneCash` (wallet) and `Kiosk` (Fawry / Aman) payment methods, with everything only they used:
  their payment-method ids, the wallet number and expiry date sent to Fawaterak, `Payment.PaymentCode`
  (migration `RemovePaymentCode`), the "code expired" webhook and its hash, `RecordPendingTransaction`,
  and the 48-hour window. Neither could be paid on Fawaterak staging. Phase 2 can add them back as new
  payment strategies. Methods are stored as text, so the existing Card and CashOnDelivery rows are unaffected.
- `PaymentResultApplier` answers `NotPaidYet` for an invoice that is still open, instead of recording it.
- A one-page map of the payment flow (the three moments A, B, C) was published as an artifact.

### Changed
- `POST /orders` takes `{ addressId, paymentMethod }`. Items come from the cart (emptied by the order), the
  shipping details from a saved address. A deleted address is 404; an empty cart or a product that is no longer
  sold is 409.
- For gateway payments the cart is emptied only once Fawaterak has returned a payment link, so a gateway
  failure leaves the cart as it was.
- `Product` has an optimistic concurrency token: PostgreSQL's `xmin`, as the shadow property `Version`
  (migration `AddProductConcurrencyToken`, which runs no SQL because `xmin` already exists).

### Turned back on
- Card payments through Fawaterak: every `// PAYMENT GATEWAY (off)` block is live again
  (`OrderService`, `FawaterakController`, `PaymentExpiryWorker`, the DI registrations).
- `GET /orders/{id}/payment/events` and `POST /orders/{id}/payment/sync`, now **admin only**.
- The return URL redirects to `Fawaterak:ClientReturnUrl` instead of the removed `/orders.html`.
- `FLOW 1`…`FLOW 9` comments trace one payment through the code; `checkout-flow.http` walks through it.

### Verified
- Against Fawaterak staging: sign up, address, cart, card order (real invoice and payment URL), same
  Idempotency-Key returns the same order, cart emptied, stock reserved, forged webhook 401, admin sync, customer
  sync 403, cash on delivery confirmed at once.

### Tests moved to PostgreSQL
- SQLite has no `xmin`, so 21 of 33 tests failed once the concurrency token existed. SQLite is gone from the
  tests: `TestDatabase` creates a fresh database per test on the local PostgreSQL (from the API's user-secrets,
  or `ConnectionStrings__Tests` in CI), applies the real migrations, and deletes it afterwards.
- All 33 tests pass. The NU1903 warning (vulnerable SQLite package) is gone with the package.

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
