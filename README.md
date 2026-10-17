# Audiophile

A full-stack e-commerce store for audio gear. ASP.NET Core 10 API with Clean Architecture,
PostgreSQL (a local install or Docker in development, Neon in production), ASP.NET Core Identity + JWT, Fawaterak payments,
and a React client.

**Start here:** [`docs/project-overview.md`](docs/project-overview.md) (what exists and why) ·
[`docs/roadmap.md`](docs/roadmap.md) (the plan) · [`docs/use-cases.md`](docs/use-cases.md) ·
[`docs/diagrams.md`](docs/diagrams.md) (architecture, ERD, state machines) ·
[`docs/database.md`](docs/database.md) (PostgreSQL, Neon, migrations) ·
[`docs/api.md`](docs/api.md) (every endpoint) · [`docs/changelog.md`](docs/changelog.md) (what changed).

This is a rebuild of an earlier single-project API ([`nourokbi/audiophile`](https://github.com/nourokbi/audiophile)),
restructured into layers. The decision log is in [`docs/build-plan.md`](docs/build-plan.md); the payment
integration is described in [`docs/fawaterak-integration.md`](docs/fawaterak-integration.md).

## Layout

```
src/
  Audiophile.Domain          entities, enums, domain rules       — depends on nothing (but Identity's base user)
  Audiophile.Application     use cases, DTOs, interfaces         — depends on Domain
  Audiophile.Infrastructure  EF Core + PostgreSQL, Identity, JWT, Fawaterak — depends on Application
  Audiophile.API             controllers, middleware, DI         — depends on both
client/                      React + Vite storefront
tests/                       unit tests and HTTP integration tests
docs/
```

The reference arrows only ever point inward. `API` referencing `Infrastructure` is the one apparent
exception: it exists so `Program.cs` can register the implementations, and for nothing else.
No controller ever `using Audiophile.Infrastructure`.

## Packages, and why they live where they do

| Layer | Package | Reason |
|---|---|---|
| Domain | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `User` derives from `IdentityUser<Guid>` (decision D-01). Nothing else. |
| Application | `Microsoft.EntityFrameworkCore` | `IAppDbContext` exposes `DbSet`s to the use cases. |
| Infrastructure | `Npgsql.EntityFrameworkCore.PostgreSQL` | The database provider: an implementation detail. |
| Infrastructure | `Microsoft.AspNetCore.Authentication.JwtBearer` | Issuing and validating tokens. |
| Infrastructure | `Microsoft.Extensions.Http.Resilience` | Timeouts, retries and circuit breaker for the payment gateway. |
| API | `Microsoft.EntityFrameworkCore.Design` | Needed by `dotnet ef` in the startup project. |
| API | `Microsoft.OpenApi` | Pinned to 2.11.0; the template's 2.0.0 has a published advisory. |

## Run it locally

Prerequisites: .NET 10 SDK, PostgreSQL (a local install or Docker), the EF CLI (`dotnet tool install -g dotnet-ef`).

```bash
cd src/Audiophile.API

# 1. Secrets: never in appsettings.json
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=audiophile;Username=postgres;Password=<yours>"
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
dotnet user-secrets set "Admin:Email" "admin@audiophile.local"
dotnet user-secrets set "Admin:Password" "<at least 8 characters, with a digit>"

# 2. Run: in Development the database is created/migrated and the admin account made on startup
dotnet run --launch-profile http          # http://localhost:5040
```

No local PostgreSQL? Skip the connection-string secret and run `docker compose up -d` from the repo root instead.
Then open `src/Audiophile.API/Audiophile.API.http` and send the requests in order. Every endpoint is described in
[`docs/api.md`](docs/api.md); production on Neon is in [`docs/database.md`](docs/database.md).

## Tests

```bash
dotnet test
```

Unit tests cover the domain rules and the use cases. The HTTP tests in `tests/Audiophile.Tests/Api`
start the whole API in memory and call it like a client would (register, log in, refresh, admin-only
endpoints). They use SQLite in place of PostgreSQL so they run without Docker.

## Migrations

The DbContext lives in Infrastructure and the startup project is API, so both must be named:

```bash
dotnet ef migrations add <Name> \
  --project src/Audiophile.Infrastructure \
  --startup-project src/Audiophile.API \
  --output-dir Persistence/Migrations
```
