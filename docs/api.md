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
| POST | `/auth/refresh` | 🔓 · 10/min per IP | `{ refreshToken }` | 200 new session | 401 |
| POST | `/auth/logout` | 🔑 | `{ refreshToken }` | 204 | 401 |
| GET | `/auth/me` | 🔑 | — | 200 `{ id, name, email, roles }` | 401 |

A **session** is `{ accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user }`.

Rules:
- **Access token:** a JWT, valid **15 minutes**. Claims: `sub` (user id), `email`, `name`, `role`, `jti`.
- **Refresh token:** a random string, valid **14 days**. Only its SHA-256 hash is stored.
- **Rotation:** each refresh token works once. `/auth/refresh` revokes it and returns a new pair.
- **Reuse detection:** presenting an already-used refresh token returns 401 **and revokes every session of that user**,
  because two parties holding the same token means it was stolen.
- **Logout** deletes that one refresh token (other devices stay signed in). It only ever touches the caller's own tokens.
- **Login errors don't say which part was wrong:** an unknown email and a wrong password get the same 401 message.
- **Lockout:** 5 wrong passwords lock the account for 5 minutes.
- **Passwords:** at least 8 characters, with a digit and a lowercase letter (ASP.NET Core Identity).
- **New accounts** get the `Customer` role and an empty cart. The `Admin` account comes from configuration
  (`Admin:Email` / `Admin:Password`) and is created at startup if missing.

## Products — `ProductsController`

| Method | Path | Access | Success | Errors |
|---|---|---|---|---|
| GET | `/products?search=&category=&minPrice=&maxPrice=&page=1&pageSize=12` | 🔓 | 200 page | 400 bad filter |
| GET | `/products/{id}` | 🔓 | 200 product | 404 |
| POST | `/products` | 👑 | 201 product | 400 · 401 · 403 |
| PUT | `/products/{id}` | 👑 | 200 product | 400 · 401 · 403 · 404 |
| DELETE | `/products/{id}` | 👑 | 204 | 401 · 403 · 404 |

- A **page** is `{ items, page, pageSize, totalCount, totalPages }`. `pageSize` is 1–50, search ≤ 100 characters,
  `minPrice` ≤ `maxPrice`. Only active products are listed, ordered by name.
- **Search** matches name or description, case-insensitive.
- A **product** is `{ id, name, description, features, price, stock, inStock, category, imageUrl, inTheBox }`;
  `inTheBox` is an object like `{ "Headphone unit": 1, "User manual": 1 }`.
- **Create/update body:** `{ name, description, features?, price, stock, category, imageUrl?, inTheBox? }`. Update replaces
  every field, including setting the stock count.
- **DELETE deactivates** the product (it disappears from the shop) instead of deleting it, because past orders point at it.

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

## Cart — `CartController` (all 🔑)

| Method | Path | Body | Success | Errors |
|---|---|---|---|---|
| GET | `/cart` | — | 200 cart | 401 |
| POST | `/cart/items` | `{ productId, quantity }` | 200 cart | 400 · 404 product · 409 not enough stock |
| PUT | `/cart/items/{productId}` | `{ quantity }` | 200 cart | 400 · 404 · 409 |
| DELETE | `/cart/items/{productId}` | — | 200 cart | 404 |
| DELETE | `/cart` | — | 200 empty cart | — |

Every call returns the whole cart: `{ items, totalQuantity, subTotal, shippingFee, total }`. Adding a product already in
the cart increases that line. Quantity per line is 1–100. Shipping is 50 EGP, free from 5,000 EGP.

## Orders — `OrdersController` (all 🔑)

| Method | Path | Success | Errors |
|---|---|---|---|
| POST | `/orders` + header `Idempotency-Key` | **201** new order · **200** same key seen before | 400 · 404 · 409 stock |
| GET | `/orders` | 200 my orders, newest first | 401 |
| GET | `/orders/{id}` | 200 | 404 |

- Body today: `{ items: [{ productId, quantity }], paymentMethod, shipping: { fullName, phone, street, city, country, postalCode? } }`.
  Phase 1 changes it to `{ addressId, paymentMethod }`, taking the items from the cart.
- Only `CashOnDelivery` is accepted while the payment gateway is switched off (roadmap Phase 2).
- Prices are read from the database, never from the request.

## Other

| Method | Path | Access | Notes |
|---|---|---|---|
| GET | `/health` | 🔓 | `{ "status": "ok" }` |
| — | `/payments/fawaterak/*` | — | Switched off until Phase 2 (see `fawaterak-integration.md`) |

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
| `Fawaterak:*` | user-secrets | Payment gateway (off until Phase 2) |
