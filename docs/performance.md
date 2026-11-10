# Performance

Load tests use [k6](https://k6.io) and the script in [`perf/products.js`](../perf/products.js). Each simulated shopper
opens one page of the shop, opens a product with its related ones, then searches, like the React client.

## How to run it

k6 runs in a container, so there is nothing to install. It calls the API directly on the Docker network.

```bash
docker compose --profile app up -d --build       # the app, see the README

docker compose run --rm k6                       # 50 shoppers who read each page for a second
VUS=100 THINK_SECONDS=0 docker compose run --rm k6   # 100 shoppers clicking nonstop, to find the limit
```

In PowerShell, set the values first: `$env:VUS=100; $env:THINK_SECONDS=0; docker compose run --rm k6`.

## Baseline, before any caching

Measured 2026-11-01 on the `postgres:17-alpine` and API containers, 72 products, no Redis. Each run takes 1 m 45 s:
30 s ramping up, 1 minute steady, 15 s ramping down. No run had a failed request.

| Load | Requests per second | All pages p95 | Shop p95 | Product p95 | Search p95 |
|---|---|---|---|---|---|
| 50 shoppers, 1 s pause per page | 52 | 8 ms | 9 ms | 5 ms | 8 ms |
| 20 shoppers, nonstop | 823 | 40 ms | 42 ms | 28 ms | 42 ms |
| 100 shoppers, nonstop | 940 | 169 ms | 177 ms | 123 ms | 176 ms |

p95 means 95% of requests were faster than this.

**What it shows:** going from 20 to 100 nonstop shoppers adds only 14% more requests per second, but p95 gets more than
4 times slower. So the limit on this setup is about 900 to 950 requests per second, and past that requests wait in a queue.
At that point PostgreSQL uses about 270% CPU (nearly 3 cores) for data that rarely changes, which is the work a cache removes.

**Limits of these numbers:**

- k6, the API and the database all share one laptop (8 logical CPUs, about 7 GB for Docker), so they compete for CPU, and
  the numbers change between machines. Compare runs made on the same machine, not absolute values.
- The catalogue is small. The shop request gets slower as products are added, and a cached response wouldn't.
- This covers reading products only. Logging in and placing orders are not measured.

## Server paging, on a catalogue of 5,323 products

Measured 2026-11-05 on real products from the Best Buy open data set, loaded only for testing. The shop used to download
the whole catalogue on every visit, which at this size is 107 requests. It now asks for the one page on screen. The script
above was rewritten to match, so the table at the top of this page describes the older behaviour on 72 products and can't be
compared with these numbers.

| 50 shoppers, same pauses | Before: whole catalogue | After: one page |
|---|---|---|
| Time to show the shop | median 21 s, p95 25 s | p95 48 ms |
| Requests to show the shop | 107 | 1 |
| Data received per second | 7.5 MB | 0.24 MB |
| PostgreSQL CPU | about 6.8 cores | about 0.8 core |

No run had a failed request.

**The new limit is search.** With 100 shoppers clicking nonstop the API handles 245 requests per second, and p95 is 1.37 s:

| Page, 100 shoppers nonstop | Median | p95 |
|---|---|---|
| Product | 52 ms | 301 ms |
| Shop | 72 ms | 1.1 s |
| Search | 896 ms | 1.71 s |

PostgreSQL uses about 7 cores here. Search read the name and description of every row, and indexes on the sort orders can't
help it.

## Search index

Measured 2026-11-05 with `pg_trgm` indexes on the lower-cased name and description, about 3 MB together.

| One query, milliseconds | Before | After |
|---|---|---|
| Count for a rare word (102 matches) | 26 | 1 |
| A word with no matches, page and count | 27 and 23 | 0.2 and 0.2 |
| First page for a common word (1,993 matches) | 27 | 0.2 |
| Count for a common word | 24 | 19 |
| Count for two letters | 26 | 23 |

| k6 | Before | After |
|---|---|---|
| 50 shoppers, search p95 | 97 ms | 52 ms |
| 100 nonstop, search median | 896 ms | 310 ms |
| 100 nonstop, search p95 | 1.71 s | 1.26 s |
| 100 nonstop, requests per second | 245 | 287 |

**What is still slow:**

- A common word still counts by reading every row, because most rows match anyway.
- At 100 nonstop shoppers the shop page got no faster: p95 went from 1.1 s to 1.52 s, one run each, as more requests got through.
  Each shop request also counts the whole catalogue, and deep pages read every row they skip.
## Connection pool

Measured 2026-11-05. The API keeps open database connections in a pool, and Npgsql's default allows 100, which is also
PostgreSQL's limit: after a 100 shopper run `psql` was refused with `too many clients already`. Compose now sets
`Maximum Pool Size=${DB_POOL_SIZE:-20}`, so a run can try other sizes with `DB_POOL_SIZE=10 docker compose --profile app up -d`.

| 100 shoppers nonstop | Pool of 100 | Pool of 20 |
|---|---|---|
| Requests per second | 259 | 270 |
| All pages p95 | 1.11 s | 551 ms |
| Slowest request | 3.71 s | 1.08 s |
| PostgreSQL memory | 300 MB | 102 MB |
| Connections open, most running at once | 64, 44 running | 20, 8 to 11 running |
| Requests that waited too long for a connection | 0 | 0 |

**What it shows:** fewer connections did more work. With 100, PostgreSQL runs 44 queries at once on 8 CPUs, so each one waits
for CPU and for memory, and every query gets slower. With 20, the extra requests wait in the API's queue for a few
milliseconds instead, and nothing was refused. A pool of 10 wasn't measured because the laptop ran out of memory.

On a hosted database, keep the pool under the plan's connection limit, minus a few for migrations and `psql`.

## Checkout under load

[`perf/checkout.js`](../perf/checkout.js) has many customers buy the same product at the same moment: add it to the cart,
check out with cash on delivery, open the order. Every purchase changes the same stock row, so it presses on the `xmin`
check and its one retry, the connection pool, and the outbox that sends each order's email and notification.

Setup creates one product per `HOT_PRODUCTS` (1 by default, the worst case) with a large stock and one customer per
shopper, then hides the products again at the end. Its logins come from one container address, so the API's rate
limits must be off for the run, and the admin login comes from `.env`.

```bash
RATE_LIMITING=false docker compose --profile app up -d api       # limits off for this run only
docker compose run --rm k6 run /scripts/checkout.js              # 20 customers, 1 s pauses
VUS=50 THINK_SECONDS=0 docker compose run --rm k6 run /scripts/checkout.js
docker compose --profile app up -d api                           # limits back on
```

**What to read in the summary:**

- `http_req_duration{step:checkout}` p95, which should stay under 1 s
- `stock_conflicts`: a 409 means the stock changed twice under one request and the customer is asked to try again;
  nothing is lost, but a high count means the single product row is the bottleneck
- After the run, Mailpit (http://localhost:8025) should hold one email per order placed, and RabbitMQ's queues
  (http://localhost:15672) should be empty, which shows the outbox kept up

**Not measured yet.** The script was written on 2026-11-10, when virtualization was off on the test laptop and
Docker couldn't start. Put the numbers here after the first run.
