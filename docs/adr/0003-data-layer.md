# ADR-0003: EF Core + SQLite (relational) with Redis for cache/counters

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
The role calls for both **relational and NoSQL** databases. The prototype must run instantly for whoever grades it, while showing a credible production path.

## Decision
- **Relational (source of truth):** **EF Core** over **SQLite** for the `ShortUrl` mappings and click records. The `DbContext` and repository interfaces are provider-agnostic so switching to **SQL Server/PostgreSQL** is a connection-string + provider change.
- **NoSQL (speed layer):** **Redis** for hot short-code → target lookups and click counters (`INCR`), behind an `ICacheStore` interface with an **in-memory fallback** when Redis isn't configured.

## Alternatives considered
- **SQLite only** — simpler, but skips the NoSQL/caching skill. Rejected.
- **SQL Server + Redis in Docker as the only path** — most realistic but can't run without Docker; adds grader friction. Rejected as default; supported via docker-compose.

## Consequences
- (+) Demonstrates relational + NoSQL and a cache-aside pattern on the read path.
- (+) Runnable with zero infra (SQLite file + in-memory cache).
- (−) SQLite ≠ production RDBMS semantics (concurrency, types) — documented as a prototype trade-off with a clear migration path.

## Update (2026-10-10, Phase 4): PostgreSQL in docker-compose
The provider is now **config-selected** (`Database:Provider` = `Sqlite` | `Postgres`) against the same `Default` connection string. docker-compose runs the app on **PostgreSQL**, exercising the relational swap for real.

Trade-off on migrations: EF migrations are provider-specific and our migration history is SQLite-only. Rather than maintain dual migration assemblies, startup uses `EnsureCreated()` for Postgres (schema built from the model) and `Migrate()` for SQLite (keeps history for tests/local). This is a deliberate prototype simplification; a production Postgres deployment would add a dedicated Postgres migrations assembly. Tests continue to run on SQLite for speed.
