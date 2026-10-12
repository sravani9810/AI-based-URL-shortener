# URL Shortener — AI-Assisted Engineering Prototype

A production-shaped URL shortener built with **.NET 10**, demonstrating AI-assisted engineering with engineer-owned decisions. Create short links, redirect them fast, and track click analytics through an event-driven pipeline — with caching, expiration, rate limiting, and graceful degradation.

> This repository is an interview assignment graded on **AI-assisted engineering judgment**. The disciplined process — decomposition, defensible decisions, traceability, validation — is documented under [`docs/`](docs/).

> 🚀 **Reviewers:** see **[docs/RUNNING.md](docs/RUNNING.md)** for step-by-step run instructions. Fastest path: `dotnet run --project src/UrlShortener.Api --no-launch-profile --urls "http://localhost:5080"` then open http://localhost:5080.

## Features
- **Minimal web UI** — a static page (served at `/`) to shorten URLs and look up analytics.
- **Create short links** — auto Base62 codes or **custom aliases**, with optional **expiration**.
- **Fast redirects** — cache-aside (Redis or in-memory); `302` / `410 Gone` (expired) / `404`.
- **Click analytics** — event-driven (Kafka or in-memory channel), off the redirect hot path; `GET .../stats` returns totals + daily buckets.
- **Rate limiting** — token-bucket per client IP on create (`429` when exceeded).
- **Resilient** — redirects survive Redis/Kafka outages; health checks; RFC-7807 errors.

## Architecture
Layered .NET solution (Domain → Application → Infrastructure → API). Full detail in [docs/architecture.md](docs/architecture.md).

![Architecture](docs/diagrams/architecture-components.png)

## Quickstart

### Run locally (no infrastructure)
SQLite + in-memory cache + in-memory events — nothing else to install.
```bash
dotnet run --project src/UrlShortener.Api --no-launch-profile --urls "http://localhost:5080"
```
- **Web UI:** open `http://localhost:5080` in a browser
- Swagger: `http://localhost:5080/swagger` · Health: `http://localhost:5080/healthz`

### Run the full stack (Postgres + Redis + Kafka)
```bash
docker compose up --build
```
API on `http://localhost:8080`. Tear down: `docker compose down -v`.

## API

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/urls` | Create a short link (`url`, optional `alias`, optional `expiresAt`) |
| `GET`  | `/{code}` | Redirect to target (`302`), `410` if expired, `404` if unknown |
| `GET`  | `/api/urls/{code}` | Link metadata |
| `GET`  | `/api/urls/{code}/stats` | Click analytics (total, last-clicked, per-day) |
| `GET`  | `/healthz` | Health check |

```bash
# Create
curl -X POST http://localhost:5080/api/urls -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com","alias":"demo","expiresAt":"2030-01-01T00:00:00Z"}'
# Redirect
curl -i http://localhost:5080/demo
# Stats
curl http://localhost:5080/api/urls/demo/stats
```

## Testing
```bash
dotnet test                         # 38 tests (unit + integration)
dotnet format --verify-no-changes   # style gate (also in CI)
```
Approach and coverage: [docs/testing.md](docs/testing.md).

## Project layout
```
src/
  UrlShortener.Domain          # entities, events, interfaces
  UrlShortener.Application     # UrlService, DTOs, validation
  UrlShortener.Infrastructure  # EF Core, Redis, Kafka, Base62
  UrlShortener.Api             # controllers, DI, Swagger, rate limiting
tests/UrlShortener.Tests       # unit + integration
docs/                          # architecture, decisions (ADRs), scenarios, summary
```

## Documentation
- [Architecture](docs/architecture.md) · [Setup](docs/setup.md) · [Testing](docs/testing.md) · [Limitations & trade-offs](docs/limitations.md)
- [Requirements & decomposition](docs/requirements.md) · [Three scenarios](docs/scenarios.md)
- [Decision log](docs/DECISIONS.md) · [ADRs](docs/adr/) · [AI-usage traceability](docs/AI-USAGE.md)
- [**Final engineering summary**](docs/engineering-summary.md)

## Tech
.NET 10 · ASP.NET Core · EF Core (SQLite/PostgreSQL) · Redis · Kafka · xUnit · Docker · GitHub Actions
