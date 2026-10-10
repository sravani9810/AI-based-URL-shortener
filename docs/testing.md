# Testing Approach

> Deliverable: testing approach (see `limitations.md` for limitations & trade-offs).

## Philosophy
Test behavior at the right level: fast, isolated unit tests for logic; full-pipeline integration tests for wiring and contracts. Time- and infra-dependent behavior is made **deterministic** (controllable clock, in-memory infra) so tests are fast and reliable.

## Layers

### Unit tests
- **Base62CodeGenerator** — round-trips, boundaries (`0`, `61`, `62`, `long.MaxValue`), negative/invalid input.
- **UrlService** — URL validation (http/https only, reject `ftp`/`javascript`/malformed), custom-alias handling + conflicts, reserved codes, expiry validation, cache-hit resolution, expiry transition (via `FakeTimeProvider`), stats aggregation.

Dependencies are faked in-memory (`FakeShortUrlRepository`, `FakeClickAnalyticsRepository`, `InMemoryCacheStore`), so these tests run with no I/O.

### Integration tests (`WebApplicationFactory`)
Boot the real API (routing, DI, EF Core, migrations) against a per-test SQLite file and a controllable clock:
- **Create → redirect** round-trip (302 + Location), 404 for unknown, 400 for invalid URL, 409 for duplicate alias.
- **Expiration** — redirect before expiry, `410 Gone` after the server clock advances (deterministic, no `sleep`).
- **Analytics** — redirects produce click events that flow through the in-memory pipeline and surface in `/stats` (polled).
- **Rate limiting** — exhausting the token bucket yields `429`.

### Manual / infra verification (documented in `AI-USAGE.md`)
Beyond automated tests, the stack was exercised against **real infrastructure** via Colima:
- Redis: cache key + TTL observed; redirect still works with Redis **down** (DB fallback).
- Kafka: `UrlClicked` messages observed on the `url-clicks` topic; stats reflected the counts.
- docker-compose: API on **PostgreSQL** + Redis + Kafka; click rows confirmed directly in Postgres.

## Running
```bash
dotnet test                      # all tests
dotnet format --verify-no-changes   # style gate (also enforced in CI)
```

Current suite: **38 tests, all passing.** CI (GitHub Actions) runs restore → format gate → build → test on every push/PR.

## What determinism buys us
- `FakeTimeProvider` makes expiry tests instant and flake-free.
- In-memory cache/event implementations let the full pipeline be tested without Redis/Kafka, while the same code paths run against real infra in compose.
