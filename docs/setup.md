# Setup & Run

> Deliverable: setup instructions.

## Prerequisites
- **.NET 10 SDK** (`dotnet --version` → 10.x)
- Optional for the full stack: **Docker** (Colima or Docker Desktop) + the Docker Compose plugin

## Option A — Run locally (zero infrastructure)
Uses SQLite + in-memory cache + in-memory event pipeline. Nothing else needed.

```bash
dotnet run --project src/UrlShortener.Api --no-launch-profile --urls "http://localhost:5080"
```

- API: `http://localhost:5080`
- Swagger (Development): `http://localhost:5080/swagger`
- Health: `http://localhost:5080/healthz`

The SQLite database (`urlshortener.db`) is created automatically via EF migrations on startup.

## Option B — Full stack via docker-compose
Brings up the API on **PostgreSQL** + **Redis** + **Kafka**, wired together.

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- Postgres: `localhost:5432` (app/app), Redis: `localhost:6379`, Kafka: `localhost:9092`
- Tear down: `docker compose down -v`

## Try it (curl)

```bash
# Create (auto code)
curl -X POST http://localhost:5080/api/urls -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com/page"}'

# Create (custom alias + expiry)
curl -X POST http://localhost:5080/api/urls -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com","alias":"demo","expiresAt":"2030-01-01T00:00:00Z"}'

# Redirect (302)
curl -i http://localhost:5080/demo

# Stats
curl http://localhost:5080/api/urls/demo/stats
```

## Configuration
Settings bind from `appsettings.json` or environment variables (double-underscore syntax).

| Key | Default | Purpose |
|-----|---------|---------|
| `ConnectionStrings:Default` | `Data Source=urlshortener.db` | Relational DB connection |
| `Database:Provider` | `Sqlite` | `Sqlite` or `Postgres` |
| `ConnectionStrings:Redis` | *(unset → in-memory cache)* | Redis endpoint, e.g. `localhost:6379` |
| `ConnectionStrings:Kafka` | *(unset → in-memory events)* | Kafka bootstrap, e.g. `localhost:9092` |

Example — run locally against Redis + Kafka:
```bash
ConnectionStrings__Redis=localhost:6379 ConnectionStrings__Kafka=localhost:9092 \
  dotnet run --project src/UrlShortener.Api --no-launch-profile --urls "http://localhost:5080"
```

## Tests
```bash
dotnet test
```
