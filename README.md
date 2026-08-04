# Audiophile API

Backend for the Audiophile e-commerce store. ASP.NET Core 10, PostgreSQL, Clean Architecture.

This is a rebuild of an earlier single-project API, restructured into layers and built
feature by feature. See [`docs/build-plan.md`](docs/build-plan.md) for the scope and the order of work.

## Layout

```
src/
  Audiophile.Domain          entities, enums, domain rules — depends on nothing
  Audiophile.Application     use cases, DTOs, interfaces   — depends on Domain
  Audiophile.Infrastructure  EF Core, repositories, Stripe — depends on Application
  Audiophile.API             controllers, middleware, DI   — depends on both
tests/
docs/
```

The reference arrows only ever point inward. `Domain` has no NuGet packages and no
project references at all — if you ever need to add one there, something has leaked
into the wrong layer.

`API` referencing `Infrastructure` is the one apparent exception. It exists so
`Program.cs` can register concrete implementations in the DI container, and for
nothing else. No controller should ever `using Audiophile.Infrastructure`.

## Packages, and why they live where they do

| Layer | Package | Reason |
|---|---|---|
| Domain | *(none)* | Pure C#. Keep it that way. |
| Application | *(none yet)* | MediatR and FluentValidation arrive in Milestone 3. |
| Infrastructure | `Microsoft.EntityFrameworkCore` | Persistence is an implementation detail. |
| Infrastructure | `Npgsql.EntityFrameworkCore.PostgreSQL` | ditto |
| Infrastructure | `BCrypt.Net-Next` | Hashing is an implementation detail — the domain gets an interface. |
| API | `Microsoft.AspNetCore.Authentication.JwtBearer` | Auth is a transport concern. |
| API | `Microsoft.EntityFrameworkCore.Design` | Needed by `dotnet ef` in the startup project. |
| API | `Microsoft.OpenApi` | Pinned to 2.11.0 — the template's 2.0.0 has a published advisory. |

## Prerequisites

- .NET 10 SDK
- PostgreSQL (local install or `docker run`)

## Running

```bash
dotnet restore
dotnet build

# secrets — never in appsettings.json
cd src/Audiophile.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=audiophile;Username=postgres;Password=..."
dotnet user-secrets set "Jwt:Key" "<at least 32 random chars>"

dotnet run
```

## Migrations

The DbContext lives in Infrastructure, the startup project is API, so both must be named:

```bash
dotnet ef migrations add <Name> \
  --project src/Audiophile.Infrastructure \
  --startup-project src/Audiophile.API \
  --output-dir Persistence/Migrations

dotnet ef database update --startup-project src/Audiophile.API
```
