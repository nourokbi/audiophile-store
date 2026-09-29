# Audiophile API — scope and build plan

Derived from a line-by-line review of the previous single-project implementation
(`audiophile`, commit `3db368f`). Requirements are grouped by the milestone that
delivers them, not by module, so you only ever hold one milestone in your head.

**Milestones group requirements. Phases decide the order you build them in.** The
two are deliberately separate — see [Delivery order](#delivery-order). Requirement
IDs never change once written; only their scheduled phase does.

## In scope

The complete shopping loop: browse a catalogue, build a cart, save an address,
check out, pay, see your orders.

## Deliberately deferred

| Deferred | Why it can wait |
|---|---|
| Order tracking microservice + RabbitMQ | A second service is only interesting once the first one is complete and deployed. Nothing in the core loop depends on it. |
| Coupons | The `Coupon` entity and its constraints were already designed in the old repo; carry the design over when you want it. Discounts are an addition to checkout, not a prerequisite. |
| ~~ASP.NET Core Identity migration~~ | **No longer deferred — done up front.** See D-01. |

Keep the old repo. These are the follow-on work, and the analysis for all
of them is already written.

---

## Delivery order

Build in **phase** order. Each row lists the requirement IDs it delivers.

| Phase | Delivers | Requirement IDs | Budget | Actual |
|---|---|---|---|---|
| **0** | Set up how you work | M0-01 … M0-06 | 2 hours | |
| **1** | Persistence and a working request | M1-03 … M1-12, M5-01 | 2–3 weeks | |
| **2** | **Ship the skeleton** | M6-01, M6-02, M6-04, M6-06, M6-07, M6-08 | 3–5 days | |
| **3** | Close the auth gaps | M2-01 … M2-06 | 1 week | |
| **4** | CQRS with MediatR | M3-01 … M3-05, M5-02 | 1–2 weeks | |
| **5** | Checkout | M4-01 … M4-17, M5-03, M5-04, M5-05 | 3–5 weeks | |
| **6** | Run it properly | M6-03, M6-05, M6-09 | 1 week | |
| **7** | Polish | M7-01, M7-03, M7-05 | 1 week | |

Budgets assume evenings and weekends, not full days. **They exist to be wrong.**
Fill the Actual column when a phase closes and write one line in the learning log
saying why the two differ. That comparison is the project-management skill this
project is meant to teach; the gap is the lesson, not a failure.

M1-01 and M1-02 are already satisfied by the scaffold. Milestone 5 has no phase of
its own — see D-04.

---

## Decision log

Decisions taken during the rebuild that change instructions written above.

### D-01 · ASP.NET Core Identity, adopted up front — 2026-08-10

Rather than deferring it, `User` now derives from `IdentityUser<Guid>`.

`Guid` rather than Identity's default `string` key, so every `UserId` foreign key in
the model is the same type as the key it points at.

Consequences: `IPasswordHasher` in `Application` is dead (Identity owns hashing);
`AppDbContext` derives from `IdentityDbContext`, not `DbContext`; the M1-12 seeder must
go through `UserManager`, not `DbContext`; and `Domain` now carries one package
reference, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.

### D-02 · Identity owns roles; the `UserRole` enum is removed — 2026-08-18

`AspNetRoles` / `AspNetUserRoles` are the source of truth for who is an admin. Roles
are read with `UserManager.GetRolesAsync` and emitted as claims when the JWT is built
in Milestone 2.

**Why:** it's what `[Authorize(Roles = …)]` expects natively, and adding a role later
is a data change rather than a migration.

**What it costs:** the domain can no longer answer "is this user an admin" — role is a
relationship row, not a property. Creating an admin becomes two operations that can
partially fail, which M1-12 has to handle.

Removed: `Domain/Enums/UserRole.cs`, and `User.Role` / `IsAdmin` / `PromoteToAdmin` /
`DemoteToCustomer`. Added: `Application/Common/Roles.cs`, string constants so neither
the attribute nor the seeder uses a magic string.

### D-03 · Validation is wired once, in Milestone 3 — 2026-08-16

M1-06 and M3-03 as written build validation twice. In Milestone 1, write the
FluentValidation validators and register them, but do **not** wire the automatic MVC
integration — it would be deleted in M3-03. Wire it once, as a MediatR pipeline
behavior, where it belongs.

### D-04 · Deploy early, test continuously, document as you go — 2026-08-26

Three changes to *sequencing*, none to scope. The engineering order was already
right; what was wrong was when the evidence of the work becomes visible.

**Deploy moves to Phase 2, right after persistence.** M6-01 (Dockerfile), M6-02
(compose), M6-04 (CI) and M6-06 (a live URL) ship a walking skeleton — `/health` and
`GET /products` — before auth exists.

*Why:* deployment problems are cheapest to fix when there is almost nothing deployed.
Discovering in month four that configuration only ever resolved because user-secrets
existed is a bad week; discovering it with two endpoints is ten minutes. Everything
after Phase 2 then deploys continuously, which is the only way the ops half is
actually learned. Deploying once, at the end, teaches you to deploy once.

M6-03 (Redis-backed rate limiting) and M6-05 (migration strategy) stay late — both
need a system that exists.

**Milestone 5 is dissolved into the milestones that create the code.**

| Was | Now runs in |
|---|---|
| M5-01 domain invariants | Phase 1 |
| M5-02 handler tests | Phase 4, per converted slice |
| M5-03 integration / Testcontainers | Phase 5 |
| M5-04 concurrency | Phase 5 |
| M5-05 webhook idempotency | Phase 5 |

*Why:* this plan calls M5-04 "the single most valuable thing in this project", and
then schedules it last — on code written weeks earlier, at the point in a solo
project where motivation is lowest. Tests-as-a-final-milestone is the most reliably
abandoned milestone there is.

More concretely: **M4-06 is not implementable without its test.** "Two concurrent
checkouts, exactly one succeeds" is not a feature you build and then verify — the
test *is* the specification. There is no way to know the retry-then-409 path works by
clicking around.

**Documentation becomes a requirement, not an afterthought.** New: M6-07 (README),
M6-08 (public OpenAPI/Swagger), M6-09 (OpenTelemetry), and Milestone 0 in full.

*Why:* nobody reads the source. A hiring engineer spends ninety seconds — README,
live link, maybe one file. The README and the decision log are the deliverable that
the code supports, not the other way round.

**Marked optional:** M7-02 (`pg_trgm` search) and M7-04 (API versioning). Versioning
on a solo project with a single consumer is ceremony, and a shallow talking point.

---

## Milestone 0 — Set up how you work

**Phase 0.** Two hours, before a line of Phase 1. All of it compounds, and none of it
is retrofittable — a commit history cannot be rewritten into existence later.

Task-level breakdown: **`docs/phase-0-tasks.md`**. That is where to start.

| ID | Requirement |
|---|---|
| M0-01 | The toolchain is verified working end to end before any of it is needed. |
| M0-02 | `Directory.Build.props` sets `TreatWarningsAsErrors`, `Nullable` and `ImplicitUsings` for every project at once. |
| M0-03 | `Directory.Packages.props` centralises package versions; no `Version=` attribute survives in any `.csproj`. |
| M0-04 | Work happens on a branch per stage, one commit per task, merged through a self-reviewed pull request. |
| M0-05 | `docs/learning-log.md` exists and gets an entry per task. |
| M0-06 | `README.md` exists in its final shape now, with sections empty, and is filled in as the project earns them. |

### M0-01 — Verify the toolchain

Run each of these and confirm it works *before* you need it. Every one of them fails
in a way that is confusing when it happens mid-task and obvious when it happens now.

```
dotnet --version                     # expect 10.x
dotnet tool install -g dotnet-ef     # or: dotnet tool update -g dotnet-ef
dotnet ef --version
psql --version                       # and connect to the server, not just the client
docker --version && docker ps        # Docker Desktop actually running
```

### M0-02 / M0-03 — Two files that stop a class of problem

`Directory.Build.props` at the repo root applies to every project beneath it. Put
`Nullable`, `ImplicitUsings` and — the important one — `TreatWarningsAsErrors` there.
Warnings-as-errors is worth it now and painful to introduce later, once there are
four hundred of them.

`Directory.Packages.props` is central package management. Versions live in one file;
`.csproj` files carry only `<PackageReference Include="…" />` with no version. The old
repo shipped EF Core `10.0.7` next to Npgsql `10.0.1` — that class of drift is exactly
what this prevents, and it is a thing most .NET portfolios do not have.

### M0-04 — Work like it's a team

The old repo has three commits, one of them `Applying OOP & SOLID`. This one has one
commit and thirty untracked files. Your git history is public and it is read.

- Branch per stage: `phase-1/stage-b-persistence`
- One commit per task, message naming the task: `B3: ProductConfiguration with jsonb InTheBox`
- Open it as a pull request against `master`, even though you're the only reviewer
- Run `/code-review` on your own PR before merging

That last step is the point. Reading your own diff as a reviewer rather than as its
author is the single skill juniors most visibly lack, and it costs nothing to practise.

### M0-05 — The learning log

`docs/learning-log.md`. One short entry per task, three lines:

```markdown
## B3 — ProductConfiguration · 2026-09-02 · est 2h / actual 5h
**Predicted:** a jsonb column would just work through the backing field.
**Happened:** it saved nothing. No ValueComparer, so EF never saw the dictionary change.
**Would tell someone else:** a mutable collection mapped to jsonb needs a comparer, or
you get silent data loss — the worst failure mode there is.
```

Six months from now you will not remember why `xmin` mattered. This file is what turns
the project into interview answers rather than a repo you once wrote.

### M0-06 — README skeleton now

Create the headings today, empty. They fill in as you go, and the empty ones are a
visible to-do list. Final shape is specified in M6-07.

---

## Milestone 1 — Restructure

**Phase 1.** Move the existing code into the four layers with **no behaviour change**.
Nothing new is added; if an endpoint responded 404 before, it responds 404 after.

Task-level breakdown: **`docs/phase-1-tasks.md`**. That is where to start.

| ID | Requirement |
|---|---|
| M1-01 | `Domain` holds entities, enums and domain rules, and references nothing. ✅ scaffold |
| M1-02 | `Application` holds DTOs and the interfaces `Infrastructure` implements. ✅ scaffold |
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
| M5-01 | Unit tests for domain invariants: `User`, `CartItem`, `Order` totals. *(pulled forward — D-04)* |

**Done when** every endpoint that worked in the old repo works here, `GET /health`
returns 200, `dotnet test` is green, and a fresh `dotnet ef database update` followed
by `dotnet run` gives you a browsable catalogue and an admin you can log in as.

### Notes on the traps

- **M1-01 and `User.UpdatePassword`.** The old `User` entity calls `BCrypt` directly.
  That makes the domain depend on a hashing library. Superseded by D-01 — Identity owns
  hashing now — but the principle stands.
- **M1-11 is not optional.** The old rate limiter partitions on
  `Connection.RemoteIpAddress`. Behind any reverse proxy — which is every deployment
  target — that is the proxy's address, so every user in the world shares one
  5-requests-per-minute bucket. It looks fine locally and fails completely in production.
- **M1-12 unblocks everything.** In the old repo `User.Role` is `init`-only and defaults
  to `Customer`, and no migration seeds anything. Every admin endpoint was unreachable
  on a fresh database.
- **M5-01 is nearly free.** The invariant checks written during the domain phase already
  exist as a scratchpad console app. Promoting them to xUnit under `tests/` is an hour.

---

## Milestone 6a — Ship the skeleton

**Phase 2.** Out of numerical order on purpose; see D-04. Nothing here is new work
invented for the plan — these are M6 requirements, pulled forward.

| ID | Requirement |
|---|---|
| M6-01 | A multi-stage `Dockerfile` producing a runtime image without the SDK. |
| M6-02 | `docker compose up` starts the API and PostgreSQL. *(Redis joins in Phase 6.)* |
| M6-04 | GitHub Actions builds and tests every pull request. |
| M6-06 | The API is deployed and reachable at a URL. |
| M6-07 | `README.md` is complete for what exists so far. |
| M6-08 | OpenAPI is served, and Swagger UI is reachable on the deployed instance. |

**Done when** you can send someone a link, they can open Swagger UI, call
`GET /products` against your live database, and it answers.

### M6-07 — What the README has to contain

In this order, because it's the order it gets read in:

1. One paragraph: what this is. **The live URL and the Swagger link, above the fold.**
2. The architecture diagram — four layers, dependency arrows pointing inward.
3. A link to the decision log in this file. Most portfolio projects have no record of
   why anything is the way it is; having one is disproportionately persuasive.
4. **The hard problems, and how they were solved.** Optimistic concurrency on stock,
   refresh-token reuse detection, asynchronous Stripe confirmation, the real client IP
   behind a proxy. Three or four paragraphs. This is the section that gets you asked
   about the right things in an interview.
5. How to run it: `docker compose up`.

### M6-08 — Why Swagger on the deployed instance matters

It is the most-clicked thing on a deployed API portfolio project, and the only way
someone evaluates an API without a front end. Guard the mutating endpoints, not the
document.

---

## Milestone 2 — Close the auth gaps

**Phase 3.** Small, contained, and worth doing before anything is built on top of auth.

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

### The JWT role-claim trap

D-02 put roles in Identity, so this is where they become claims. `[Authorize(Roles =
"Admin")]` does not look for a claim called `role` — it looks for `ClaimTypes.Role`,
the URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. Emit `"role"`
and authorization fails silently: a valid token, the right role in the database, a 403,
and nothing in the logs. Either emit `ClaimTypes.Role`, or emit `"role"` and set
`TokenValidationParameters.RoleClaimType = "role"`. Pick one and be consistent.

---

## Milestone 3 — CQRS with MediatR

**Phase 4.** Convert one vertical slice at a time. Start with Products, which is already
complete, so you are changing shape and not behaviour.

| ID | Requirement |
|---|---|
| M3-01 | Every use case is a `Command` or `Query` with exactly one handler. |
| M3-02 | Controllers parse the request, call `mediator.Send`, and map the result. Nothing else. |
| M3-03 | A validation pipeline behavior runs the FluentValidation validator for any request that has one. |
| M3-04 | A logging pipeline behavior records handler name, duration and outcome. |
| M3-05 | Products, Cart, Addresses and Auth are all converted. |
| M5-02 | Unit tests for handlers, with dependencies substituted — written with each slice. |

**Done when** no controller action contains an `if` about business rules, and adding a
new cross-cutting concern means writing one behavior rather than editing every handler.

---

## Milestone 4 — Checkout

**Phase 5.** The real work, and the part with no existing code to lean on. The `Order`,
`OrderItem` and `Payment` entities and all the order DTOs were already designed in the
old repo — carry them over.

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
| M5-03 | Integration tests over the checkout flow using `WebApplicationFactory` and `Testcontainers.PostgreSql`. |
| M5-04 | A concurrency test: two simultaneous checkouts, one unit of stock, exactly one winner. |
| M5-05 | A webhook idempotency test: the same event delivered twice confirms the order once. |

**Done when** you can register, add to cart, check out with Stripe's test card, and see
the order move to `Confirmed` because a webhook said so — and M5-04 and M5-05 both fail
if you remove the code that makes them pass.

### The two things to get right

**M4-05 / M4-06 — stock.** The old repo already configures a Postgres `xmin` optimistic
concurrency token on `Product`, and nothing uses it. Wire it into the decrement: load the
product, subtract, save, and catch `DbUpdateConcurrencyException` when someone else got
there first. Retry once, then return 409.

**Write M5-04 first.** Per D-04, the test is the specification here — there is no way to
verify "exactly one winner" by clicking around. That test is the single most valuable
thing in this project: it is the difference between having read about optimistic
concurrency and having shipped it.

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

## Milestone 6b — Run it properly

**Phase 6.** The half of Milestone 6 that needs a real system to exist.

| ID | Requirement |
|---|---|
| M6-03 | Rate-limit counters live in Redis, so they stay correct across replicas. |
| M6-05 | Migrations have a decided deployment path — a CI step, or a guarded startup migration. |
| M6-09 | OpenTelemetry traces and a hosted log sink, so a production problem is diagnosable without a debugger. |

**Done when** you can answer "why was that request slow in production" by looking,
not by guessing.

### On Redis

There are two separate arguments for it, and only one of them is interesting.
The dull one is caching the catalogue. The good one is this: the rate limiter holds
its counters in process memory, so the moment you run two replicas behind a load
balancer they stop agreeing and the limit silently stops working. Redis is what makes
it correct again. That is a correctness argument, not a performance one, and it is the
better answer in an interview.

### On M6-09 — the one library worth adding

Every .NET portfolio has Serilog. Very few have distributed tracing. Traces across the
request → handler → EF Core → Stripe path, exported to any hosted backend, cost an
afternoon and give you a genuinely uncommon sentence: *"I could see which handler was
slow in production, and it was the N+1 in order history."*

---

## Milestone 7 — Polish

**Phase 7.**

| ID | Requirement |
|---|---|
| M7-01 | Catalogue listing is paginated with a capped page size. |
| M7-03 | Catalogue reads are cached in Redis, cache-aside, invalidated on write. |
| M7-05 | Read-only queries use `AsNoTracking` and project rather than materialising entities. |
| M7-02 | *(optional)* Product search uses a `pg_trgm` GIN index or a generated `tsvector`, not `LIKE '%term%'`. |
| M7-04 | *(optional)* API versioning. |

---

## What this project deliberately does not teach

The failure mode of "learn new architectures for my CV" is a zoo of half-used
technologies. The stack here — Clean Architecture, CQRS/MediatR, a rich domain model,
EF Core, Identity + JWT rotation, Stripe, optimistic concurrency, Testcontainers,
Docker, CI, OpenTelemetry — is one coherent story you can defend end to end. Depth in
one stack interviews better than breadth across five.

So: no event sourcing, no gRPC, no service mesh, no second database engine. RabbitMQ
and the tracking microservice stay deferred until the first service is complete and
deployed, which is exactly when a second one becomes interesting rather than decorative.

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
