# API reference

Base URL locally: `http://localhost:5040`. Every request and response body is JSON; enums are strings
(`"Headphones"`, `"CashOnDelivery"`). To try them, use `src/Audiophile.API/Audiophile.API.http`.
In Development the OpenAPI document is at `/openapi/v1.json`.

🔓 open to everyone · 🔑 needs `Authorization: Bearer <accessToken>` · 👑 needs the `Admin` role

## Auth — `AuthController`

| Method | Path | Access | Body | Success | Errors |
|---|---|---|---|---|---|
| POST | `/auth/register` | 🔓 · 5/hour per IP | `{ name, email, password }` | **201** session | 400 bad name/email/password · 409 email taken · 429 |
| POST | `/auth/login` | 🔓 · 10/min per IP | `{ email, password }` | 200 session | 401 · 429 |
| POST | `/auth/refresh` | 🍪 · 30/min per IP | — | 200 new session | 401 no cookie or a bad one |
| POST | `/auth/logout` | 🍪 | — | 204, the cookie is deleted | — |
| GET | `/auth/me` | 🔑 | — | 200 `{ id, name, email, roles }` | 401 |

A **session** body is `{ accessToken, accessTokenExpiresAt, user }`. The refresh token never appears in a body: register,
login and refresh set it as the cookie `audiophile_refresh` (🍪), which is `HttpOnly`, `SameSite=Strict`, `Path=/auth`,
and `Secure` when the request came over https. The browser sends it back by itself, page scripts cannot read it.

Rules:
- **Access token:** a JWT, valid **15 minutes**. Claims: `sub` (user id), `email`, `name`, `role`, `jti`.
- **Refresh token:** a random string, valid **14 days**. Only its SHA-256 hash is stored.
- **Rotation:** each refresh token works once. `/auth/refresh` revokes it and returns a new pair.
- **Reuse detection:** presenting an already-used refresh token returns 401 **and revokes every session of that user**,
  because two parties holding the same token means it was stolen.
- **Logout** deletes the refresh token in the cookie (other devices stay signed in) and needs no access token, so an
  expired one still logs out. Holding the cookie is the proof of the session, and `SameSite=Strict` stops other sites.
- **Login errors don't say which part was wrong:** an unknown email and a wrong password get the same 401 message.
- **Lockout:** 5 wrong passwords lock the account for 5 minutes.
- **Passwords:** at least 8 characters, with a digit and a lowercase letter (ASP.NET Core Identity).
- **New accounts** get the `Customer` role and an empty cart. The `Admin` account comes from configuration
  (`Admin:Email` / `Admin:Password`) and is created at startup if missing.

## Products — `ProductsController` (browsing) and `AdminProductsController` (changes)

| Method | Path | Access | Success | Errors |
|---|---|---|---|---|
| GET | `/products?category=&minPrice=&maxPrice=&page=1&pageSize=12` | 🔓 | 200 page | 400 bad filter |
| GET | `/products/search?q=&page=1&pageSize=12` | 🔓 | 200 page | 400 empty or long `q` |
| GET | `/products/{id}` | 🔓 | 200 product | 404 |
| GET | `/admin/products?category=&minPrice=&maxPrice=&q=&availability=All&lowStockOnly=false&sort=Name&page=1&pageSize=12` | 👑 | 200 page, hidden products included | 400 · 401 · 403 |
| GET | `/admin/products/search?q=&page=1&pageSize=12` | 👑 | 200 page, hidden products included | 400 · 401 · 403 |
| POST | `/admin/products` | 👑 | 201 product | 400 · 401 · 403 |
| PUT | `/admin/products/{id}` | 👑 | 200 product | 400 · 401 · 403 · 404 |
| DELETE | `/admin/products/{id}` | 👑 | 204 | 401 · 403 · 404 |
| POST | `/admin/products/{id}/restock` | 👑 | 200 product | 400 quantity below 1 · 401 · 403 · 404 |
| PUT | `/admin/products/{id}/price` | 👑 | 200 product | 400 price not positive · 401 · 403 · 404 |
| PUT | `/admin/products/{id}/availability` | 👑 | 200 product | 401 · 403 · 404 |

- A **page** is `{ items, page, pageSize, totalCount, totalPages }`. `pageSize` is 1–50, `q` ≤ 100 characters,
  `minPrice` ≤ `maxPrice`. `/products` lists only active products, ordered by name; `/admin/products` lists hidden ones
  too, which is how the dashboard's product table reads the catalogue.
- **Admin filters** on `GET /admin/products`: `availability` is `All`, `OnSale` or `Hidden`; `lowStockOnly=true` keeps
  products with 8 or fewer left; `sort` is `Name`, `Newest`, `PriceLowToHigh`, `PriceHighToLow` or `StockLowToHigh`
  (ties sorted by name); `q` searches like `/search`. An unknown `availability` or `sort` is a 400.
- **Search** matches name or description, case-insensitive. It is a contains search that works like
  `LIKE '%term%'` and reads every row; a `pg_trgm` or full text index can replace it when the catalogue grows.
- A **product** is `{ id, name, description, features, price, stock, inStock, category, imageUrl, inTheBox, isActive, createdAt }`;
  `category` is `Headphones`, `Speakers`, `Earphones`, `Amplifiers` or `Accessories`;
  `inTheBox` is an object like `{ "Headphone unit": 1, "User manual": 1 }`.
- **Create body:** `{ name, description, features?, price, stock, category, imageUrl?, inTheBox?, slug? }`.
- **Update body:** `{ name, description, features?, price, category, imageUrl?, inTheBox? }`. It never changes the
  stock, which only moves through restock and orders, so an edit made with an old number cannot undo a checkout.
  `inTheBox` left out keeps the current contents, an empty object empties the box.
- Every product has a **`slug`**, its address in the shop (`/headphones/xx59-headphones`). It is unique, made
  from the name when `POST /admin/products` leaves it out, and never changed by an update. The client matches its
  pages to the API's products by slug.
- **DELETE deactivates** the product (it disappears from the shop) instead of deleting it, because past orders point at it.
  It is the same as `availability` with `{ isActive: false }`, which can also bring it back with `true`.
- **Restock, price and availability** reach hidden products too, with bodies `{ quantity }`, `{ price }` and `{ isActive }`.
  Restock adds to the stock and retries once when a checkout changes the same product at the same moment.
  So does `PUT /admin/products/{id}`, and editing never changes whether a product is shown.

## Addresses — `AddressesController` (all 🔑)

| Method | Path | Success | Errors |
|---|---|---|---|
| GET | `/addresses` | 200 list | 401 |
| GET | `/addresses/{id}` | 200 | 401 · 404 |
| POST | `/addresses` | 201 | 400 · 401 |
| PUT | `/addresses/{id}` | 200 | 400 · 401 · 404 |
| DELETE | `/addresses/{id}` | 204 | 401 · 404 |

Body: `{ fullAddressName, street, city, country, phone, postalCode? }`. Each customer only ever sees their own addresses:
someone else's address is a **404**, not a 403, so ids can't be probed. DELETE is a soft delete.

## Cart — `CartController` (all 🔑 except the quote)

| Method | Path | Body | Success | Errors |
|---|---|---|---|---|
| GET | `/cart` | — | 200 cart | 401 |
| POST | `/cart/items` | `{ productId, quantity }` | 200 cart | 400 · 404 product · 409 not enough stock |
| PUT | `/cart/items/{productId}` | `{ quantity }` | 200 cart | 400 · 404 · 409 |
| DELETE | `/cart/items/{productId}` | — | 200 cart | 404 |
| DELETE | `/cart` | — | 200 empty cart | — |
| POST | `/cart/quote` (anonymous) | `[{ productId, quantity }]` | 200 cart | 400 more than 100 lines |
| POST | `/cart/merge` | `[{ productId, quantity }]` | 200 cart | 400 more than 100 lines · 401 |

Every call returns the whole cart: `{ items, totalQuantity, subTotal, shippingFee, total }`. Adding a product already in
the cart increases that line. Quantity per line is 1–100. Shipping is 50 EGP, free from 5,000 EGP.

A visitor keeps the cart in the browser. `quote` prices it without an account and `merge` moves it into the account
after login. Both leave out hidden or unknown products and cap each line at the stock instead of failing, so the
shop saves back what `quote` returns and a login never fails because of the cart.

## Orders — `OrdersController` (all 🔑)

| Method | Path | Success | Errors |
|---|---|---|---|
| POST | `/orders` + header `Idempotency-Key` | **201** new order · **200** same key seen before | 400 · 404 address · 409 empty cart / stock / product no longer sold · 502 / 504 gateway |
| GET | `/orders` | 200 my orders, newest first | 401 |
| GET | `/orders/{id}` | 200 | 404 |
| POST | `/orders/{id}/cancel` | 200 the cancelled order, stock returned | 404 · 409 already cancelled, shipped, or a paid card order (contact support) |
| POST | `/orders/{id}/payment/retry` | 200 the order with a new `payment.checkoutUrl`, or confirmed if the last attempt was paid after all | 404 · 409 not an unpaid card order, or its 30 minutes ran out |
| GET | `/orders/{id}/payment/events` | 200 what Fawaterak sent about this order | 🛡 admin · 404 |
| POST | `/orders/{id}/payment/sync` | 200 the order after asking Fawaterak | 🛡 admin · 404 · 409 not a gateway order |

- Body: `{ addressId, paymentMethod }`. The items come from the signed-in user's cart, which is emptied when the
  order is placed; the address is copied into the order. `paymentMethod`: `CashOnDelivery` or `Card`.
- `CashOnDelivery` is `Confirmed` at once. `Card` stays `Pending` and the response carries
  `payment.checkoutUrl` (Fawaterak's payment page) until the webhook confirms it.
- Prices are read from the database, never from the request.
- Two checkouts for the last unit at the same moment: one gets the order, the other a 409 (`xmin` plus one retry).
- The full journey, step by step, is in `src/Audiophile.API/checkout-flow.http`; `docs/checkout-flow.md` follows it through the code.

## Admin orders — `AdminOrdersController` (all 👑)

| Method | Path | Success | Errors |
|---|---|---|---|
| GET | `/admin/orders?status=&from=&to=&q=&page=1&pageSize=10` | 200 page plus `statusCounts` | 400 · 401 · 403 |
| GET | `/admin/orders/stats?days=30` | 200 the overview numbers | 400 days not 1–365 · 401 · 403 |
| GET | `/admin/orders/{id}` | 200 order with `shipping` and `items` | 401 · 403 · 404 |
| PUT | `/admin/orders/{id}/status` | 200 the changed order | 400 unknown status · 401 · 403 · 404 · 409 not a next status, or Confirmed before the card payment succeeded |

- Every customer's orders, newest first. A row has `customerName`, `customerEmail`, `paymentStatus`, `itemCount`
  (the sum of the quantities) and `nextStatuses`, the buttons the dashboard shows.
- **Filters**: `status` is one status; `from` and `to` are UTC days like `2026-10-25`, and `to` includes its whole day;
  `q` (up to 100 characters) matches the customer's name or email, or an order id typed from its start.
- `statusCounts` is `{ "Pending": 0, "Confirmed": 3, ... }` for the search and dates but **not** the status, so every tab
  keeps its number while one tab is open.
- **Status body**: `{ "status": "Shipped" }`. The steps are Pending → Confirmed → Processing → Shipped → Delivered, and
  Cancelled from any step before Shipped. `Cancelled` returns the stock and fails a payment still pending; a paid card
  order can be cancelled here, and its refund is then made by hand on Fawaterak. A card order leaves `Confirmed` out of
  its `nextStatuses` until the payment succeeded, the webhook or *Sync with gateway* confirms it. A cash order's
  payment becomes `Succeeded` when the order is `Delivered`, the courier collected the cash.
- **Stats**: the period is `days` whole **Cairo days** ending today (`Africa/Cairo`, summer time included).
  - `revenue`: the total of orders placed in the period whose payment `Succeeded` and that are not `Cancelled`;
    `previousRevenue` is the same for the `days` days before; `paidOrderCount` is how many orders that is
  - `orderCount`: every order placed in the period; `activeOrders`: every order now Pending, Confirmed, Processing or
    Shipped whatever its date; `customerCount`: everyone who ever placed an order
  - `perDay`: one `{ day, orders, revenue }` per day of the period, oldest first, today last, empty days as 0
  - `byPaymentMethod` `{ paymentMethod, orders, revenue }` and `byCategory` `{ category, unitsSold, revenue }`, every
    method and category listed; `topProducts` the 5 best `{ productId, productName, unitsSold, revenue }`. Category and
    product revenue add up the items, so they leave the shipping out
  - `statusCounts`: the orders placed in the period by their status now, every status listed
- The `from` and `to` filters of `GET /admin/orders` are Cairo days too.

## Other

| Method | Path | Access | Notes |
|---|---|---|---|
| GET | `/health` | 🔓 | `{ "status": "ok" }` |
| POST | `/payments/fawaterak/webhook_json` | 🔓 signed | Fawaterak's webhook; 401 when the `hashKey` is wrong (see `fawaterak-integration.md`) |
| GET | `/payments/fawaterak/return` | 🔓 | Where the browser lands after paying; redirects to `Fawaterak:ClientReturnUrl` |

## Errors

Every error is an RFC 7807 **ProblemDetails** body: `{ type, title, status, detail }`. Stack traces never leave the server.

| Status | Raised by |
|---|---|
| 400 | Invalid input: a domain rule (`ArgumentException`) or a malformed request |
| 401 | Not signed in, wrong credentials, or an invalid / expired / reused refresh token |
| 403 | Signed in, but without the required role |
| 404 | Not found, or not yours |
| 409 | Not allowed in the current state: not enough stock, email taken, illegal status change |
| 429 | Rate limit reached |
| 502 / 504 | Payment gateway error / timeout |
| 500 | A bug; the details are only in the server log |

## Configuration

| Key | Where | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | user-secrets / env var | PostgreSQL connection string (see `database.md`) |
| `Database:MigrateOnStartup` | appsettings | `true` in Development, `false` in production |
| `Jwt:Key` | user-secrets / env var | Signing key, at least 32 characters. The app refuses to start without it |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | appsettings | Token settings |
| `Admin:Email`, `Admin:Password`, `Admin:Name` | user-secrets / env var | The admin account created at startup |
| `Cors:AllowedOrigins` | appsettings | Origins allowed to call the API (`http://localhost:5173` in Development) |
| `Fawaterak:*` | user-secrets | Payment gateway. `ClientReturnUrl` (default `http://localhost:5173/orders`) is where customers land after paying |
