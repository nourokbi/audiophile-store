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

PostgreSQL uses about 7 cores here. Search reads the name and description of every row, and indexes on the sort orders can't
help it. A trigram index is the fix to try next.

## After caching

To be measured with the same commands once Redis and HybridCache are in.
