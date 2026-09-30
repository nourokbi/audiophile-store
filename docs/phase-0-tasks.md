# Phase 0 — Set up how you work

Covers **M0-01 … M0-06**. Roughly two hours. Do it before Stage A of
`phase-1-tasks.md`.

**Why this phase exists at all.** Everything here is retrofittable in theory and
miserable in practice. A commit history cannot be written after the fact.
`TreatWarningsAsErrors` costs nothing today and costs a weekend once there are four
hundred warnings. A learning log started in month three is missing the two months
where you learned the most.

None of this is code you're here to learn. It's the scaffolding that makes the code
you *are* here to learn worth something afterwards.

---

## Order of work

```
P0. Commit what already exists   5 min    ← do this first, today
M0-01 Verify the toolchain       15 min   fail now, not mid-task
M0-02 Directory.Build.props      10 min   one place for compiler settings
M0-03 Directory.Packages.props   20 min   one place for versions
M0-04 The git workflow           15 min   branch, commit, self-reviewed PR
M0-05 docs/learning-log.md       10 min   the file that outlives the project
M0-06 README skeleton            20 min   headings now, content as you earn it
```

---

## ☐ P0 — Commit what already exists

**This is the most urgent thing in the repo and it isn't in any milestone.**

Right now there are 28 uncommitted files and one commit in the entire history. Your
whole domain model — nine entities, the value objects, `Guard`, everything behind
decisions D-01 and D-02 — exists only on one disk, unversioned.

```bash
git checkout -b phase-0/setup
git add .
git commit -m "Domain model: entities, value objects, guards; Identity adopted (D-01, D-02)"
```

**Done when:** `git status` is clean and `git log --oneline` shows two commits.

---

## ☐ M0-01 — Verify the toolchain

Run all five. Each one fails in a way that is baffling mid-task and obvious now.

```bash
dotnet --version                      # expect 10.x
dotnet tool install -g dotnet-ef      # or: dotnet tool update -g dotnet-ef
dotnet ef --version
psql --version
docker --version && docker ps
```

Two of these lie if you only read the version:

- **`psql --version` proves the client exists, not the server.** Actually connect:
  `psql -h localhost -U postgres -c "select version();"`. B1 needs a *reachable*
  server, and "command not found" versus "connection refused" are different problems
  you don't want to be diagnosing at the same time as a connection string.
- **`docker --version` works with the engine stopped.** `docker ps` is the one that
  proves Docker Desktop is actually running. You don't need it until Phase 2, but
  finding out it's broken now costs nothing.

**Done when:** all five run, and `psql` returns a server version string.

---

## ☐ M0-02 — `Directory.Build.props`

Create it at the repo root, beside `Audiophile.slnx`.

MSBuild walks up from every `.csproj` looking for this file and imports the first one
it finds. Anything in it applies to all four projects at once — and to the test
projects you haven't created yet, which is the point.

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

</Project>
```

Then **delete `TargetFramework`, `Nullable` and `ImplicitUsings` from all four
`.csproj` files** — they're inherited now, and a duplicated setting is a setting that
will eventually disagree with itself.

**`TreatWarningsAsErrors` is the one that matters.** A nullable warning is the
compiler telling you a reference can be null on a path you didn't think about. As a
warning it scrolls past; as an error you deal with it. The cost of turning it on is
proportional to how much code already exists, which is why now and not later.

**`EnforceCodeStyleInBuild`** makes analyzer style rules build failures too. Optional,
and reasonable to skip if it turns out to be noisy — note the decision either way.

**Predict first:** if `Directory.Build.props` sets `TargetFramework` to `net10.0` and
a `.csproj` also sets it to something else, which wins? Write your answer, then test
it by temporarily changing one.

**Done when:** `dotnet build` succeeds with 0 warnings, and no `.csproj` mentions
`TargetFramework`.

---

## ☐ M0-03 — `Directory.Packages.props`

Central Package Management. Versions live in exactly one file; `.csproj` files say
what they need, never which version.

Create at the repo root:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
    <PackageVersion Include="Microsoft.OpenApi" Version="2.11.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>

</Project>
```

Then strip every `Version="…"` attribute out of the four `.csproj` files. A reference
becomes:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

Keep the `Microsoft.EntityFrameworkCore.Design` child elements (`PrivateAssets`,
`IncludeAssets`) in the `.csproj` — those are per-project, only the version moves.

**Why this is worth twenty minutes.** The old repo shipped `Microsoft.EntityFrameworkCore`
10.0.7 next to `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1. Nothing warned. EF Core
and its provider are versioned in lockstep by design, and a mismatch surfaces as a
`MissingMethodException` at runtime rather than a build error. Central versions make
that class of drift structurally impossible, and it's a thing most .NET portfolios
don't have.

**Trap:** leaving a `Version=` attribute on a `PackageReference` while central
management is on is a build error (NU1008), not a silent override. That's the feature
working — but it means you must strip all four projects, not three.

**Done when:** `dotnet build` succeeds and `grep -r 'PackageReference.*Version=' src/`
returns nothing.

---

## ☐ M0-04 — The git workflow

The old repo has three commits, one of them `Applying OOP & SOLID`. This repo had one
until an hour ago. Your history is public and it is read — by anyone deciding whether
to interview you.

For the rest of the project:

- **Branch per stage:** `phase-1/stage-b-persistence`
- **Commit per task**, message naming it:
  `B3: ProductConfiguration — jsonb InTheBox with order-insensitive ValueComparer`
- **Open it as a pull request against `master`**, even though you're the only reviewer
- **Run `/code-review` on your own diff before merging**

The PR step is the one people skip and the one that carries the value. Reading your
own change as a reviewer — diff-first, no memory of what you meant — is a different
activity from writing it, and it is the single skill juniors most visibly lack. It
also gives every stage a place where the reasoning is written down.

Set up the remote now if there isn't one:

```bash
gh repo create audiophile-api --private --source=. --remote=origin
git push -u origin master
```

Keep it private until Phase 2 has something deployed to point at, then make it public.

**Done when:** there is a remote, `master` is pushed, and you're working on a branch.

---

## ☐ M0-05 — `docs/learning-log.md`

Create it with a header and nothing else. One entry per task from here on.

```markdown
# Learning log

One entry per task. Written *after* the task, not during.

---

## B3 — ProductConfiguration · 2026-09-02 · est 2h / actual 5h

**Predicted:** a jsonb column would just work through the backing field.

**Happened:** it saved nothing. `SaveChanges` reported 0 rows and didn't error. No
`ValueComparer`, so EF never noticed the dictionary had changed.

**Would tell someone else:** a mutable collection mapped to jsonb needs a comparer or
you get silent data loss — the worst failure mode there is, because nothing tells you.
```

Three lines, written after the task while it's still annoying. **Estimate and actual
on every entry** — that's M0-04's other half and it's what makes the phase budgets in
`build-plan.md` mean anything.

**Why bother.** Six months from now you will not remember why `xmin` mattered, or what
`MapInboundClaims` broke. This file is the difference between a project you can talk
about for forty minutes and a repo you once wrote. The "would tell someone else" line
is deliberate — it's the same act as explaining it in an interview, practised early.

**Done when:** the file exists, and P0 through M0-04 each have an entry. Yes, log
Phase 0 too — it's the practice run.

---

## ☐ M0-06 — README skeleton

Create the final headings now, mostly empty. The empty ones are a visible to-do list,
and filling one in is a real milestone marker.

```markdown
# Audiophile API

> One paragraph: what this is.
> **Live:** _(Phase 2)_ · **API docs:** _(Phase 2)_

## Architecture
_(diagram — four layers, arrows pointing inward)_

## Decisions
See [the decision log](docs/build-plan.md#decision-log).

## The hard problems
_(Phase 5: optimistic concurrency on stock · refresh-token reuse detection ·
asynchronous Stripe confirmation · the real client IP behind a proxy)_

## Running it
_(Phase 2: `docker compose up`)_

## Tech
.NET 10 · PostgreSQL · EF Core · ASP.NET Core Identity · JWT · MediatR · Stripe ·
Serilog · xUnit + Testcontainers · Docker
```

Full spec for what each section eventually contains is M6-07 in `build-plan.md`.

**Also fix the rule that's now false** — this is Stage A's A4, and it's cheaper to do
while you're already in the file. `README.md` currently says `Domain` has no NuGet
packages and "Pure C#. Keep it that way." Since D-01 it references
`Microsoft.AspNetCore.Identity.EntityFrameworkCore`. State the rule *and* its one
exception, with the reason.

A rule the repo openly breaks trains you to ignore rules. Either it's true or it's
documentation debt.

**Done when:** the README's stated rules match what the `.csproj` files actually do.

---

## Phase 0 is done when

1. `git log --oneline` shows real commits and `master` is pushed to a remote
2. `dotnet build` succeeds with **0 warnings** — and warnings are errors
3. No `.csproj` contains a `Version=` attribute or a `TargetFramework`
4. `psql` connects to a running server and `docker ps` answers
5. `docs/learning-log.md` has entries for the Phase 0 tasks
6. `README.md` has its final headings, and says nothing untrue

Then go to `docs/phase-1-tasks.md`, **Stage A**. A1 and A3 are already done; what's
left is A2 (move `UserSecretsId` to the API project) and A4 (done above, if you did
it). Then B1, and the real work starts.
