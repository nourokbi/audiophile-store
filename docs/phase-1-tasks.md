# Phase 1 — Persistence and a working request

Covers **M1-03 … M1-12** and **M5-01**. You write all the code; I review, point at
bugs, and answer "why is it done this way". Tick boxes as you go.

**Do Phase 0 first — `docs/phase-0-tasks.md`.** Two hours of setup: commit what
already exists, toolchain check, `Directory.Build.props`, central package management,
the git workflow, the learning log, the README skeleton. None of it is retrofittable,
and Stage A below assumes the branch-and-PR habit is already in place.

**The rule for every task:** before you write it, write down in one line what you
think the answer is. Then build it. A wrong prediction followed by the right
answer sticks; the right answer alone slides off.

**How to ask for a review:** "review B3" or "why does my ValueComparer not fire?".
I won't fix it — I'll tell you where to look.

---

## Order of work

```
A. Clear the decks        ~1 hour     dead code, decisions
B. Persistence            1–2 days    ← the real work of this phase
C. One request end to end ~half day   proves the stack
D. Cross-cutting          ~1 day      errors, logs, health, CORS
E. Admin seeder           ~1 hour     unblocks every admin endpoint
F. Tests (M5-01)          ~1 hour     promote the scratchpad checks to xUnit
```

Do not reorder B and C. A vertical slice built before the model is settled gets
rewritten when the model changes.

One branch per stage, one commit per task, merged through a PR you review yourself
(M0-04). A learning-log entry per task, with your estimate and what it actually took
(M0-05).

---

## Stage A — Clear the decks

Small, boring, and blocking. The Identity decision left debris; remove it before
you build on top of it.

### ☐ A1 — Delete what Identity made redundant

- Delete `src/Audiophile.Application/Abstractions/IPasswordHasher.cs`
- Remove `BCrypt.Net-Next` from `Audiophile.Infrastructure.csproj`

**Why:** Identity ships `IPasswordHasher<TUser>` and `UserManager` owns hashing.
Two hashing paths is how you end up with accounts that can't log in.

**Done when:** `dotnet build` is clean and `BCrypt` appears in no `.csproj`.

---

### ☐ A2 — Move `UserSecretsId` to the API project

**What user-secrets is.** A per-developer JSON file that lives *outside* the repo, at
`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`. It's how you keep a connection
string password or a JWT signing key off disk-in-git while still having it available
in Development. It is not encrypted and it is not for production — production reads
environment variables or a key vault.

**What `UserSecretsId` in the `.csproj` does.** That GUID is the folder name. At build
time the SDK turns it into an assembly-level attribute:

```
[assembly: UserSecretsId("08c0f11e-a187-4da6-bb8b-afbaa4a8f788")]
```

**Why it has to be on API and not Infrastructure.** `WebApplication.CreateBuilder(args)`
loads user-secrets by reading that attribute off the **entry assembly** — the one with
`Main`, which is `Audiophile.API.dll`. Right now the attribute is baked into
`Audiophile.Infrastructure.dll`, where nothing ever looks for it. So today:

- `Audiophile.API.dll` has no `UserSecretsId` → `CreateBuilder` finds nothing → **no
  secrets are loaded at all**
- there is no error, no warning, no log line
- `builder.Configuration["ConnectionStrings:Postgres"]` returns `null`, and you find
  out via a `NullReferenceException` or a "host cannot be null" from Npgsql, several
  layers away from the actual cause
- `dotnet user-secrets set … -p src/Audiophile.API` fails outright, because the CLI
  needs the id on the project you point it at

**The edit:** cut the `<UserSecretsId>` line out of `Audiophile.Infrastructure.csproj`
and paste it into the `<PropertyGroup>` of `Audiophile.API.csproj`. Keep the same GUID
and the existing secrets folder comes with it — there's already a `secrets.json` there
from an earlier session.

**Done when:** `dotnet user-secrets list -p src/Audiophile.API` runs without error,
and `Audiophile.Infrastructure.csproj` no longer mentions secrets.

---

### ☐ A3 — Apply the role decision: **Identity roles win**

**Decided 2026-08-18.** ASP.NET Core Identity owns roles. The `UserRole` enum is
removed from the domain; `AspNetRoles` / `AspNetUserRoles` are the source of truth.
Roles are read out with `UserManager.GetRolesAsync` and emitted as claims when the
JWT is built in Milestone 2.

*(I'd recommended the enum. You chose Identity — it's the more conventional answer,
it's what `[Authorize(Roles=…)]` expects natively, and adding a third role later
becomes a data change instead of a migration. Noted and moving on.)*

**Edits, in this order:**

1. `src/Audiophile.Domain/Entities/User.cs`
   - remove `Role`, `IsAdmin`, `PromoteToAdmin`, `DemoteToCustomer`
   - remove the `UserRole role` parameter from the private constructor
   - collapse `Register` and `CreateAdmin` into a single `Create(name, email)` factory
2. Delete `src/Audiophile.Domain/Enums/UserRole.cs`
3. Add `src/Audiophile.Application/Common/Roles.cs` — `public const string Admin = "Admin";`
   and `Customer` — so `[Authorize(Roles = Roles.Admin)]` and the seeder never use a
   magic string. Application, not Domain: roles are now an authorization concern,
   and the domain no longer knows what a role is.

**The conceptual shift, which is the real content of this task:** role used to be an
intrinsic property set at construction — `User.CreateAdmin(…)` produced an admin in
one atomic step. It is now a *relationship row* created after the user exists. Making
an admin is two operations that can partially fail, which is why E1 gets more careful.

**What you give up:** the domain can no longer answer "is this an admin". Today
nothing in the domain asks, so the cost is zero — but if a domain rule ever needs it,
it must be *told* rather than *ask*.

**Done when:** `dotnet build` is clean, `grep -r UserRole src/` returns nothing, and
`docs/build-plan.md` records the decision.

---

### ☐ A4 — Fix the README's false rule

`README.md` says Domain references nothing. Domain now carries
`Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Amend the rule to state the
one exception and its reason.

**Why this matters more than it looks:** a rule the repo openly breaks trains you
to ignore rules. Either the rule is true or it's documentation debt.

---

## Stage B — Persistence

This is the phase. It is decision-dense and it's where I'd spend most of your time.

### ☐ B1 — Connection string and user-secrets

`appsettings.json` currently has no `ConnectionStrings` section at all.

- Put the **key** in `appsettings.json` with a local-dev-only value
- Put anything real in user-secrets: `dotnet user-secrets set "ConnectionStrings:Postgres" "…" -p src/Audiophile.API`

**Done when:** you can print the resolved connection string from `Program.cs`.

---

### ☐ B2 — Write `AppDbContext`

Location: `src/Audiophile.Infrastructure/Persistence/AppDbContext.cs`

Three things to get right:

1. **Base class** — `IdentityDbContext<User, IdentityRole<Guid>, Guid>`, per the A3 decision. Not `DbContext`, and not `IdentityUserContext` (that one omits the role tables you've just decided to use). This brings **seven** tables you didn't write: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`. Expect them in the migration; their presence is how you know the base call ran.
2. **`base.OnModelCreating(builder)` must be the first line of your override.** Skip it and Identity's own table configuration never runs. Your first migration will be garbage and hand-editing it is not worth attempting — you'd delete and start over.
3. **`builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`** so you never have to remember to register a configuration class.

**A real design decision — which types get a `DbSet<T>`?**

> Only aggregate roots. `Product`, `Order`, `Cart`, `Address` (arguably), and
> `User` via Identity. **Not** `CartItem`, `OrderItem`, `RefreshToken`, `Payment` —
> they're reachable only through their root, and a `DbSet` is an open invitation to
> query around the aggregate boundary you spent yesterday building.
>
> EF will still map them; it discovers them through the navigations. A missing
> `DbSet` costs you nothing and buys you an enforced boundary.

**Predict first:** if you remove `DbSet<CartItem>`, does `CartItem` still get a
table? Write your answer down, then check the migration.

**Done when:** `dotnet ef dbcontext info -p src/Audiophile.Infrastructure -s src/Audiophile.API` succeeds.

---

### ☐ B3 — `ProductConfiguration` — start here

`Product` is the densest entity in your model. Do it first; everything after is easier.

Location: `src/Audiophile.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`

What it needs:

- `Price` → `decimal(18,2)`
- `Category` → `.HasConversion<string>()` (enums as strings — readable in psql, survives reordering the enum)
- `Name`, `Description`, `Features` → max lengths matching the `const`s on the entity
- `InTheBox` → `jsonb`, **through the backing field**, **with a `ValueComparer`**
- `UseXminAsConcurrencyToken()` — the Npgsql extension. Creates a shadow property; you never declare `xmin` on the entity.
- CHECK constraints: `Stock >= 0`, `Price > 0`
- Index on `Category`, and on `IsActive` if you'll filter by it

**Three traps, in order of how much time they'll cost you:**

1. **No `ValueComparer` → EF never notices you changed the dictionary.** You call `PutInBox`, `SaveChanges` reports 0 rows affected, and nothing errors. Silent data loss is the worst failure mode there is.
2. **The comparer's `snapshot` lambda must rebuild the dictionary with `StringComparer.OrdinalIgnoreCase`.** The entity deliberately uses a case-insensitive dictionary. A snapshot built with the default comparer quietly loses that, and `"Headphone Unit"` vs `"headphone unit"` become two entries again.
3. **`InTheBox` has no setter** — it's `IReadOnlyDictionary` over `_inTheBox`. You must point EF at the field (`.HasField("_inTheBox")` / `PropertyAccessMode.Field`) or it can't materialise a loaded product.

**Predict first:** what is the *equality* half of the comparer for a dictionary
where key order doesn't matter? Write it before you look it up.

**Done when:** you load a product, `PutInBox("Cable", 2)`, `SaveChanges`, reload
in a fresh context, and the cable is there.

---

### ☐ B4 — `Cart` and `CartItem` configuration

- Backing field `_cartItems` for the `CartItems` navigation
- **Unique index on `(CartId, ProductId)`** — this is the database half of the rule `Cart.AddItem` enforces in memory. Both halves, always.
- **Unique index on `Cart.UserId`** — one cart per user
- CHECK: `Quantity BETWEEN 1 AND 100` (mirror `CartItem.MaxQuantity`)
- Delete behaviours: `User → Cart` and `Cart → CartItem` cascade; `CartItem → Product` **restrict**

**Why restrict on `CartItem → Product`:** cascading would silently empty people's
carts when an admin deletes a product. Restrict turns that into a loud error, at
which point you'll realise products should be deactivated (`IsActive`), not deleted —
which is why `Product.Deactivate()` exists.

---

### ☐ B5 — `Order`, `OrderItem`, `Payment` configuration

- All money → `decimal(18,2)`: `SubTotal`, `DiscountAmount`, `ShippingFee`, `TotalAmount`, `UnitPrice`, `Payment.Amount`
- `Status`, `PaymentMethod`, `PaymentStatus` → strings
- **Unique index on `Order.IdempotencyKey`** — M4-04 depends on this existing
- `Order` ↔ `Payment` is one-to-one, FK on `Payment.OrderId`
- Snapshot columns (`ShippingFullName` … `ShippingPostalCode`) — plain columns, `ShippingPostalCode` nullable, the rest required
- Delete behaviours: `Order → OrderItem` cascade; `Order → Payment` cascade; `User → Order` **restrict**; `OrderItem → Product` **restrict**

**Why restrict on `User → Order`:** an order is a financial record. Deleting a user
must not erase what they bought. This forces you to think about account deletion
properly later instead of discovering the problem in production.

**Note:** `Order` has no `UpdatedAt` trigger — the entity's `Touch()` handles it.
Don't add a database default that fights the domain.

---

### ☐ B6 — `User`, `Address`, `RefreshToken` configuration

- Three backing-field collections on `User`: `_addresses`, `_orders`, `_refreshTokens`
- `User` ↔ `Cart` — one-to-one, **required** (the `User` constructor creates it)
- Unique index on `RefreshToken.Token`
- Index on `RefreshToken.UserId`
- `Address.IsActive` — soft delete

**After A3, there is no `Role` column to configure.** `AspNetUsers` gets `Name` and
`CreatedAt` from you and nothing else role-shaped; roles live in `AspNetUserRoles`,
which `base.OnModelCreating` configures entirely. Don't add a configuration class for
the Identity tables — you'd be fighting code that already works.

**Decision I'd push back on if you reach for it:** a global query filter on
`Address.IsActive`. It looks clean and then it silently hides rows in the one place
you needed them (admin views, order history joins), with no compiler error.
`User.FindAddress` already filters. Skip the global filter.

---

### ☐ B7 — Verify the model *before* you migrate

Do not skip this. It's five minutes and it catches the expensive mistakes.

- `dotnet ef dbcontext info` succeeds
- Dump the model debug view and actually read it
- **Find the line proving `CartItems` uses the field `_cartItems`** and not the property. If EF quietly picked the property, every collection in your model is broken and the migration will still look fine.

**Done when:** you can point at the specific line in the debug view that proves
field access. Not "it probably worked."

---

### ☐ B8 — First migration

```
dotnet ef migrations add InitialCreate -p src/Audiophile.Infrastructure -s src/Audiophile.API
```

**Read the generated migration file line by line before you apply it.** This is the
single highest-value review in Phase 1 — it's the first time your intent and EF's
interpretation are visible side by side.

Check specifically:
- `InTheBox` is `jsonb`, not `text`
- money columns are `numeric(18,2)`, not `numeric`
- enum columns are `text`, not `integer`
- `xmin` appears nowhere as a real column (it's a system column — if EF tried to create one, your concurrency config is wrong)
- the Identity tables are present and sane
- unique indexes exist for `(CartId, ProductId)`, `Cart.UserId`, `RefreshToken.Token`, `Order.IdempotencyKey`

Then `dotnet ef database update`, and inspect in psql: `\d products`.

**Done when:** the database exists and `\d products` matches what you intended,
column by column.

---

## Stage C — One request, end to end

Now prove the stack. Small and fast, because the hard thinking is behind you.

### ☐ C1 — `Program.cs`: DbContext + Identity DI

The template `Program.cs` is untouched. Add `AddDbContext`, the Identity
registration, and authentication. Nothing else yet.

### ☐ C2 — Seed sample products

Plain `DbContext` seeding, idempotent (do nothing if any product exists).
The **admin user** seeder is E1 — it needs `UserManager`, which needs C1 done.

*Deviation from the plan:* M1-12 puts all seeding last. Split it. Sample products
now gives you something to look at in C3; the admin user genuinely has to wait.

### ☐ C3 — `GET /products` and `GET /products/{id}`

- `AsNoTracking`
- **Project to a DTO in `Application`.** Do not return entities from a controller — you'd serialise navigation properties, leak `Stock` to anonymous users, and couple your wire format to your domain model.
- 404 for an unknown id, not an empty 200

**Done when:** `curl http://localhost:5xxx/products` returns your seeded catalogue.

---

## Stage D — Cross-cutting

### ☐ D1 — ProblemDetails middleware (M1-05)

**The decision to make here:** your domain throws framework exception types —
`ArgumentException`, `InvalidOperationException`, `ArgumentOutOfRangeException`.
Do you map those directly, or introduce domain exception types?

> My recommendation: map them for now, and note the tension. `InvalidOperationException`
> is doing double duty — "insufficient stock" (→ 409) and "illegal status transition"
> (→ 422) are the same C# type with different HTTP meanings. You'll feel that pain in
> Phase 4 and that's the right time to introduce `DomainException` subtypes, when you
> have concrete cases instead of speculation.

A starting map: `ArgumentException` → 400 · `KeyNotFoundException` → 404 ·
`InvalidOperationException` → 409 · `DbUpdateConcurrencyException` → 409 ·
everything else → 500 **with the message suppressed outside Development**.

**Trap:** returning `ex.Message` for an unhandled exception leaks connection
strings and table names to the internet.

### ☐ D2 — FluentValidation validators (M1-06, M1-07)

Write the validators and register them in DI. **Do not wire the automatic MVC
integration.** M3-03 moves validation into a MediatR pipeline behavior — wiring it
twice means writing throwaway code. You still learn FluentValidation here; you just
skip the part you'd delete.

### ☐ D3 — Serilog + correlation id (M1-08)

Structured logging means `Log.Information("Order {OrderId} placed", id)` — a
queryable field, not string concatenation. The correlation id goes in a middleware
early in the pipeline and into the log context for the whole request.

### ☐ D4 — `GET /health` with a database check (M1-09)

Add `AspNetCore.HealthChecks.NpgSql`. Health must actually open a connection —
a health check that returns 200 while the database is down is worse than none.

### ☐ D5 — CORS from configuration (M1-10)

Origin list read from config, never `AllowAnyOrigin()` alongside credentials
(the browser rejects that combination anyway, which is a good hint it's wrong).

### ☐ D6 — Forwarded headers before the rate limiter (M1-11)

**This is the ordering task, and the plan is right that it's not optional.**

Write out your full middleware order and justify each position. Then check it
against these three:

- `UseForwardedHeaders` must come **before** anything that reads the client IP
- `UseAuthentication` before `UseAuthorization` (one populates what the other reads)
- exception handling first, so it can catch everything after it

**Why it matters:** the rate limiter partitions on `RemoteIpAddress`. Behind any
reverse proxy — which is every real deployment — that's the *proxy's* address, so
every user on earth shares one bucket. Works locally, fails completely in production.
Classic bug: invisible until it isn't.

---

## Stage E — Admin seeder

### ☐ E1 — Seed an admin from configuration (M1-12)

After A3 this is **three** idempotent steps, not one, and each can fail independently:

1. `RoleManager.RoleExistsAsync` → `CreateAsync` for `Admin` and `Customer`
2. `UserManager.FindByEmailAsync` → `CreateAsync` for the admin user
3. `UserManager.AddToRoleAsync(user, Roles.Admin)`

- Reads email + password from configuration (user-secrets, not `appsettings.json`)
- **Goes through `UserManager.CreateAsync`, never `DbContext.Add`**
- Check `IdentityResult.Succeeded` after *every* call — these methods return failures,
  they don't throw. Ignoring the result is how you get a seeder that logs nothing and
  seeds nothing.

**The partial-failure case A3 created:** step 2 succeeds and step 3 fails, leaving a
user with no role — an account that exists, can log in, and can do nothing. Make the
seeder re-entrant so the next startup repairs it: don't skip step 3 just because the
user already exists.

**Trap:** inserting a `User` straight through `DbContext` leaves `NormalizedEmail`
and `SecurityStamp` null. Nothing errors. The row looks fine in psql. Login then
fails forever with no useful message, because `UserManager` looks users up by
`NormalizedEmail`.

**Why this unblocks everything:** without it, every admin endpoint is unreachable
on a fresh database and you can't test M1-07 or anything in M4.

---

## Stage F — Tests

### ☐ F1 — Promote the invariant checks to xUnit (M5-01)

Pulled forward from Milestone 5 by D-04. It's an hour, because the hard part is
already written.

The 54 domain invariant checks currently live in a scratchpad console app. Create
`tests/Audiophile.Domain.Tests/` (xUnit + FluentAssertions), add it to the solution,
and move them over — one `[Fact]` or `[Theory]` per invariant, named for the rule it
protects: `Create_WithNameShorterThanMinimum_Throws`.

**Why it's here and not at the end:** these tests are the safety net for everything
in Phases 3–5. A refactor that quietly breaks `Cart.AddItem` is cheap to catch now
and expensive to catch in checkout. And `dotnet test` has to be green before Phase 2
wires it into CI (M6-04) — CI with no tests to run is theatre.

**Done when:** `dotnet test` passes from the repo root, and deleting a guard clause
in `User.cs` makes exactly one test fail.

---

## Phase 1 is done when

1. `dotnet ef database update` on an empty database produces the full schema
2. `dotnet run` starts clean
3. `GET /health` returns 200 and genuinely fails when Postgres is stopped
4. `GET /products` returns seeded products
5. A deliberately broken request returns a ProblemDetails body, never a stack trace
6. You can log in as the seeded admin
7. `dotnet test` is green from the repo root
8. `docs/learning-log.md` has an entry per task, with estimate and actual
9. You can explain, without notes, why `OrderItem → Product` is `Restrict`

Item 9 is not filler. If you can't answer it out loud, the model isn't in your head
yet — and Phase 5 is where that costs you.

**Then go straight to Phase 2 and deploy it.** Per D-04, the walking skeleton goes
live before auth exists. Do not carry an undeployed system into Phase 3.

---

## One thing to carry into Milestone 2

You said you'll put the role in the JWT. When you get there, this is the trap that
eats an afternoon:

`[Authorize(Roles = "Admin")]` does not look for a claim called `role`. It looks for
`ClaimTypes.Role`, which is the URI
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. Write `"role"` as your
claim type when building the token and authorization silently fails — a valid token,
a correct role in the database, and a 403 with nothing in the logs.

Two ways out, pick one and be consistent:

- emit the claim as `ClaimTypes.Role`, or
- emit short `"role"` and set `TokenValidationParameters.RoleClaimType = "role"`

Also decide whether `MapInboundClaims` stays on. `JwtSecurityTokenHandler` rewrites
short claim names to those long URIs by default; the newer `JsonWebTokenHandler`
doesn't. Two handlers with different defaults is why this bug is so common.

---

## Where I'd expect you to get stuck

Ranked by likelihood, so you know it's normal rather than a sign you're lost:

1. **The `InTheBox` `ValueComparer`** (B3) — nearly everyone gets a silently-not-saving dictionary the first time
2. **Backing fields not picked up** (B7) — silent, and the migration looks correct anyway
3. **Identity + `Guid` key + `base.OnModelCreating`** (B2) — loud, at least
4. **Middleware order** (D6) — silent locally, broken in production

Ask on any of them. Getting stuck for twenty minutes is learning; getting stuck for
three hours is just being stuck.
