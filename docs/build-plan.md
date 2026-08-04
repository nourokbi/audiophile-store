# Audiophile API — scope and build plan

Derived from a line-by-line review of the previous single-project implementation
(`audiophile`, commit `3db368f`). Requirements are grouped by the milestone that
delivers them, not by module, so you only ever hold one milestone in your head.

## In scope

The complete shopping loop: browse a catalogue, build a cart, save an address,
check out, pay, see your orders.

## Deliberately deferred

| Deferred | Why it can wait |
|---|---|
| Order tracking microservice + RabbitMQ | A second service is only interesting once the first one is complete and deployed. Nothing in the core loop depends on it. |
| Coupons | The `Coupon` entity and its constraints were already designed in the old repo; carry the design over when you want it. Discounts are an addition to checkout, not a prerequisite. |
| ASP.NET Core Identity migration | The hand-rolled auth works. Only the refresh-token rotation gap needs fixing now, which is a small change in place. Migrating to Identity is its own project, best done when checkout is finished. |

Keep the old repo. These three are the follow-on work, and the analysis for all
three is already written.

---

## Milestone 1 — Restructure

Move the existing code into the four layers with **no behaviour change**. Nothing
new is added; if an endpoint responded 404 before, it responds 404 after.

| ID | Requirement |
|---|---|
| M1-01 | `Domain` holds entities, enums and domain rules, and references nothing. |
| M1-02 | `Application` holds DTOs and the interfaces `Infrastructure` implements. |
| M1-03 | `Infrastructure` holds `AppDbContext`, EF configurations and service implementations. |
| M1-04 | `API` holds controllers, middleware, and all DI registration. |
| M1-05 | Unhandled exceptions produce an RFC 7807 `ProblemDetails`, never a bare 500 or a stack trace. |
| M1-06 | Request validation is declared once per DTO with FluentValidation and runs before the handler. |
| M1-07 | The validation currently inline in `ProductsController` moves into validators. |
| M1-08 | Structured logging with Serilog, and a correlation id on every request. |
| M1-09 | `GET /health` reports the API and its database connection. |
| M1-10 | A CORS policy admits the front end's origin, read from configuration. |
| M1-11 | `UseForwardedHeaders` runs before the rate limiter, so the limiter sees the real client IP. |
| M1-12 | A seeder creates an admin user from configuration if none exists, plus sample products. |

**Done when** every endpoint that worked in the old repo works here, `GET /health`
returns 200, and a fresh `dotnet ef database update` followed by `dotnet run` gives
you a browsable catalogue and an admin you can log in as.

### Notes on the traps

- **M1-01 and `User.UpdatePassword`.** The old `User` entity calls `BCrypt` directly.
  That makes the domain depend on a hashing library. Define `IPasswordHasher` in
  `Application`, implement it in `Infrastructure`, and pass the hash in.
- **M1-11 is not optional.** The old rate limiter partitions on
  `Connection.RemoteIpAddress`. Behind any reverse proxy — which is every deployment
  target — that is the proxy's address, so every user in the world shares one
  5-requests-per-minute bucket. It looks fine locally and fails completely in production.
- **M1-12 unblocks everything.** In the old repo `User.Role` is `init`-only and defaults
  to `Customer`, and no migration seeds anything. Every admin endpoint was unreachable
  on a fresh database.

---

## Milestone 2 — Close the auth gaps

Small, contained, and worth doing before anything is built on top of auth.

| ID | Requirement |
|---|---|
| M2-01 | Refreshing rotates: the presented refresh token is revoked and a replacement issued, in one transaction. |
| M2-02 | Refresh tokens carry a family id and a `ReplacedBy` link. |
| M2-03 | Presenting an already-revoked token revokes its entire family and returns 401. |
| M2-04 | `POST /auth/logout` requires authentication and only revokes tokens belonging to the caller. |
| M2-05 | A user can change their display name and their password. |
| M2-06 | Registration returns `201 Created`, not `200 OK`. |

**Done when** a refresh returns a *new* refresh token, replaying the old one kills
the session, and there is a test proving both.

### Why M2-03 matters

Rotation alone is half a mitigation. If an attacker steals a refresh token and uses it,
they get a fresh one and can keep rotating forever — the theft is invisible. Reuse
detection is what turns rotation into an actual defence: the moment a token is presented
twice, you know it leaked, and you kill everything in that family.

---

## Milestone 3 — CQRS with MediatR

Convert one vertical slice at a time. Start with Products, which is already complete,
so you are changing shape and not behaviour.

| ID | Requirement |
|---|---|
| M3-01 | Every use case is a `Command` or `Query` with exactly one handler. |
| M3-02 | Controllers parse the request, call `mediator.Send`, and map the result. Nothing else. |
| M3-03 | A validation pipeline behavior runs the FluentValidation validator for any request that has one. |
| M3-04 | A logging pipeline behavior records handler name, duration and outcome. |
| M3-05 | Products, Cart, Addresses and Auth are all converted. |

**Done when** no controller action contains an `if` about business rules, and adding a
new cross-cutting concern means writing one behavior rather than editing every handler.

---

## Milestone 4 — Checkout

The real work, and the part with no existing code to lean on. The `Order`, `OrderItem`
and `Payment` entities and all the order DTOs were already designed in the old repo —
carry them over.

| ID | Requirement |
|---|---|
| M4-01 | `POST /orders/preview` returns line items and totals from the current cart, persisting nothing. |
| M4-02 | `POST /orders` places an order from the cart, a chosen address and a payment method. |
| M4-03 | The order freezes a snapshot of the shipping address and of each line's name, price and image. |
| M4-04 | An idempotency key already seen returns the original order with 200, creating nothing. |
| M4-05 | Placing an order decrements `Product.Stock` inside the checkout transaction. |
| M4-06 | Two concurrent checkouts for the last unit: exactly one succeeds, the other gets 409. |
| M4-07 | Prices and stock are re-read at checkout, never trusted from the cart. |
| M4-08 | A confirmed order clears the cart. |
| M4-09 | Card payment creates a Stripe PaymentIntent and returns its client secret; the order is `Pending`. |
| M4-10 | `POST /payments/webhook` verifies the Stripe signature and moves the order to `Confirmed`. |
| M4-11 | The webhook tolerates the same event arriving twice. |
| M4-12 | Cash on delivery confirms the order without any Stripe round-trip. |
| M4-13 | A customer can list their own orders and read one by id, and no one else's. |
| M4-14 | A customer can cancel an order that has not shipped; stock is restored. |
| M4-15 | Order status transitions are validated against the state machine; illegal ones are rejected. |
| M4-16 | Cart supports setting a line's quantity directly, and clearing the whole cart. |
| M4-17 | The shipping fee comes from a written rule, not a hard-coded `50`. |

**Done when** you can register, add to cart, check out with Stripe's test card, and see
the order move to `Confirmed` because a webhook said so.

### The two things to get right

**M4-05 / M4-06 — stock.** The old repo already configures a Postgres `xmin` optimistic
concurrency token on `Product`, and nothing uses it. Wire it into the decrement: load the
product, subtract, save, and catch `DbUpdateConcurrencyException` when someone else got
there first. Retry once, then return 409. Then write the test that fires two checkouts at
one unit of stock. That test is the single most valuable thing in this project — it is the
difference between having read about optimistic concurrency and having shipped it.

**M4-09 / M4-10 — how Stripe actually works.** Creating a PaymentIntent does **not** take
payment. It returns a `client_secret`. The browser confirms the card against Stripe
directly, and Stripe tells you it succeeded through a webhook that arrives later — possibly
seconds later, possibly twice, possibly after a retry. So:

```
POST /orders          → order saved as Pending, 201 + client_secret
browser → Stripe      → card confirmed
Stripe → POST /payments/webhook → signature verified → order becomes Confirmed
```

Anything that waits for confirmation inside the `POST /orders` handler is waiting for a
message that will never arrive.

---

## Milestone 5 — Tests

| ID | Requirement |
|---|---|
| M5-01 | Unit tests for domain invariants: `User`, `CartItem`, `Order` totals. |
| M5-02 | Unit tests for handlers, with dependencies substituted. |
| M5-03 | Integration tests over the checkout flow using `WebApplicationFactory` and `Testcontainers.PostgreSql`. |
| M5-04 | A concurrency test: two simultaneous checkouts, one unit of stock, exactly one winner. |
| M5-05 | A webhook idempotency test: the same event delivered twice confirms the order once. |

**Done when** M5-04 and M5-05 both fail if you remove the code that makes them pass.

---

## Milestone 6 — Containerise and deploy

| ID | Requirement |
|---|---|
| M6-01 | A multi-stage `Dockerfile` producing a runtime image without the SDK. |
| M6-02 | `docker compose up` starts the API, PostgreSQL and Redis. |
| M6-03 | Rate-limit counters live in Redis, so they stay correct across replicas. |
| M6-04 | GitHub Actions builds and tests every pull request. |
| M6-05 | Migrations have a decided deployment path — a CI step, or a guarded startup migration. |
| M6-06 | The API is deployed and reachable at a URL. |

**Done when** there is a link you can put on a CV.

### On Redis

There are two separate arguments for it, and only one of them is interesting.
The dull one is caching the catalogue. The good one is this: the rate limiter holds
its counters in process memory, so the moment you run two replicas behind a load
balancer they stop agreeing and the limit silently stops working. Redis is what makes
it correct again. That is a correctness argument, not a performance one, and it is the
better answer in an interview.

Redis belongs here rather than earlier, because this is the first milestone where
`docker compose` exists to run it.

---

## Milestone 7 — Polish

| ID | Requirement |
|---|---|
| M7-01 | Catalogue listing is paginated with a capped page size. |
| M7-02 | Product search uses a `pg_trgm` GIN index or a generated `tsvector`, not `LIKE '%term%'`. |
| M7-03 | Catalogue reads are cached in Redis, cache-aside, invalidated on write. |
| M7-04 | API versioning. |
| M7-05 | Read-only queries use `AsNoTracking` and project rather than materialising entities. |

---

## Carried over from the old repo — keep these

Verified in the previous implementation and worth preserving rather than rewriting:

- `xmin` optimistic concurrency on `Product` — finally gets used in M4-05
- Thirteen CHECK constraints across six tables, including the conditional coupon one
- Six unique indexes: user email, cart-per-user, product-per-cart, coupon code, refresh token, idempotency key
- Delete behaviour chosen per relationship, each with a comment saying why
- `jsonb` column for `InTheBox` with a correct order-insensitive `ValueComparer`
- Money as `decimal(18,2)` everywhere; enums persisted as strings
- Snapshot columns on `Order` and `OrderItem`, all `init`-only
- `ClockSkew = TimeSpan.Zero` on token validation
- Fail-fast when the JWT key is missing, and secrets in user-secrets rather than `appsettings.json`
- Login failures that do not distinguish unknown email from wrong password

## Do not carry over

- `Copoun.cs` — the file name is misspelled; the class is `Coupon`
- The `Microsoft.SqlServer.Server` package reference — unused, and this is a Postgres project
- `Audiophile.API.csproj.lscache` — a build artefact that was committed
- The four commented-out blocks in `AddressService`
- `AddressService.DeleteAddressAsync` returning `true` for an address that does not exist — it makes the controller answer 204 where every other endpoint answers 404
- `CartService`'s cart lookup projecting to a bare `Guid` — a missing cart becomes `Guid.Empty` and the insert then fails a foreign key as an unhandled 500. Project to `Guid?`.
