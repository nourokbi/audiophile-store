# Database: PostgreSQL locally, Neon in production

One database engine everywhere: **PostgreSQL**. Locally it is either a PostgreSQL install on your machine
(this machine has PostgreSQL 18 as a Windows service) or the Docker container; in production it is
[Neon](https://neon.tech) (serverless PostgreSQL with a free tier). The code doesn't change between them.
Only the connection string does.

```mermaid
flowchart LR
    subgraph Local["Your machine"]
        API1["dotnet run<br/>(Development)"] --> PG[("PostgreSQL<br/>local install or docker compose")]
    end
    subgraph Prod["Production"]
        API2["API container"] --> Neon[("Neon<br/>PostgreSQL 17")]
    end
    CI["GitHub Actions"] -->|"migration bundle"| Neon
```

## Where the connection string comes from

| Environment | Source | Value |
|---|---|---|
| Development, local install | **user-secrets** (overrides the line below) | `Host=localhost;Port=5432;Database=audiophile;Username=postgres;Password=<yours>` |
| Development, Docker | `appsettings.Development.json` (committed; matches `docker-compose.yml`) | `Host=localhost;Port=5432;Database=audiophile;Username=audiophile;Password=audiophile` |
| Production | Environment variable `ConnectionStrings__Default` | The Neon string (see below) |
| Tests | `TestDatabase` (tests project) | A new `audiophile_test_<guid>` database per test on the same server as `ConnectionStrings:Default` (or `ConnectionStrings__Tests` in CI), migrated, then deleted |

`appsettings.json` deliberately has an empty `ConnectionStrings:Default`. The app refuses to
start without one, so a missing setting fails at deploy time, not on the first request.

## What the first migration creates

`Persistence/Migrations/…_InitialCreate.cs` creates every table and seeds the fixed data:

- **Tables:** Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, …), `Addresses`, `RefreshTokens`,
  `Products`, `Carts`, `CartItems`, `Orders`, `OrderItems`, `Payments`, `PaymentEvents`.
- **Seed data (`HasData` in the entity configurations):** the `Admin` and `Customer` roles and 4 sample products.
- **Not seeded:** the admin *account*. Its password is a secret, so it can't live in a migration. It's created at
  startup from `Admin:Email` / `Admin:Password` (`DatabaseInitializer`), only if it doesn't exist yet.

PostgreSQL-specific details worth knowing:
- `Products.InTheBox` is a `jsonb` column.
- Check constraints back up the domain rules (`Price > 0`, `Stock >= 0`, quantity 1–100, amounts ≥ 0).
- Money is `numeric(18,2)`; dates are `timestamp with time zone`, always UTC.

## Local: a PostgreSQL install (what this machine uses)

PostgreSQL 18 runs as the Windows service `postgresql-x64-18` on port 5432. Store its login in user-secrets,
which live outside the repo and override `appsettings.Development.json`:

```bash
cd src/Audiophile.API
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=audiophile;Username=postgres;Password=<your password>"
dotnet run --launch-profile http
```

You don't need to create the `audiophile` database yourself: applying the first migration creates it.
To browse it, use pgAdmin (installed with PostgreSQL) or `"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d audiophile`.
To start again from empty: `dotnet ef database drop --startup-project src/Audiophile.API`, then run the app.

## Local: Docker (alternative)

Use this instead of a local install (on a machine without PostgreSQL, or in CI). Stop the local service first,
or change the port in `docker-compose.yml`, because both want 5432. Remove the user-secret too, so the Docker
connection string from `appsettings.Development.json` is used.

```bash
docker compose up -d                 # starts PostgreSQL 17 on localhost:5432
dotnet run --project src/Audiophile.API --launch-profile http
```

In Development, `Database:MigrateOnStartup` is `true`, so the app applies migrations itself.
To start again from an empty database: `docker compose down -v` (the `-v` deletes the data volume).

## Production: Neon

### 1. Create the database (once)

1. Sign up at neon.tech → **New project** → name `audiophile`, PostgreSQL 17, the region closest to your API host.
2. In **Connection details**, copy two connection strings:
   - **Pooled** (host contains `-pooler`) is the one **the app** uses. It survives many short connections.
   - **Direct** (no `-pooler`) is the one **migrations** use. Schema changes should not go through the pooler.
3. Neon shows URLs (`postgresql://user:pass@host/db?sslmode=require`). Npgsql wants the key=value form:

```
Host=ep-xxxx-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=...;SSL Mode=Require
```

### 2. Apply migrations to Neon

Production does **not** migrate on startup (`MigrateOnStartup` is `false` outside Development).
If two API instances started together, both would try to migrate at once. Migrations are a deploy step:

```bash
# from your machine, today:
dotnet ef database update \
  --project src/Audiophile.Infrastructure --startup-project src/Audiophile.API \
  --connection "<the DIRECT Neon connection string>"

# in CI, from Phase 7: build a self-contained migration bundle and run it before deploying
dotnet ef migrations bundle --project src/Audiophile.Infrastructure --startup-project src/Audiophile.API -o efbundle
./efbundle --connection "$NEON_DIRECT_CONNECTION_STRING"
```

### 3. Configure the API host

Set these environment variables on the hosting platform (never in a committed file):

| Variable | Value |
|---|---|
| `ConnectionStrings__Default` | the **pooled** Neon string |
| `Jwt__Key` | 32+ random characters (`openssl rand -base64 48`) |
| `Admin__Email`, `Admin__Password` | the first admin account |
| `Cors__AllowedOrigins__0` | the client's URL, e.g. `https://audiophile.example.com` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Neon notes

- **Cold start:** on the free tier Neon suspends after a few idle minutes. The first query after that takes about
  half a second longer. Nothing to handle in code.
- **Branches:** Neon can branch a database like git. A `dev` branch is a free, disposable copy for trying out a
  migration before it touches production data.

## Changing the schema (every time)

1. Change an entity or an `…Configuration.cs`.
2. `dotnet ef migrations add <WhatChanged> --project src/Audiophile.Infrastructure --startup-project src/Audiophile.API --output-dir Persistence/Migrations`
3. **Read the generated migration** before committing it. A rename that EF thinks is "drop column + add column" loses data.
4. Run the app locally (it migrates itself), then commit the entity change and the migration together.
5. Deploy: run the migration against Neon, then deploy the API.
