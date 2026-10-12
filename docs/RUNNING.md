# How to Run — Assessment Guide

This guide is for reviewers. It explains how to run the URL Shortener service, verify it works, and run the tests. Two options are provided; **Option A is the fastest path to a running service.**

All commands are run from the repository root: `AI-based-URL-shortener/`.

---

## Prerequisites

| Option | Requirement |
|--------|-------------|
| **A — Local** | **.NET 10 SDK** only |
| **B — Full stack** | .NET 10 SDK **+ Docker** (Docker Desktop, or Colima + Docker Compose plugin) |

Check the SDK is installed:
```bash
dotnet --version        # expect 10.x
```
If missing: install from https://dotnet.microsoft.com/download, or on macOS `brew install dotnet`.

---

## Option A — Run locally (recommended, zero infrastructure)

Uses **SQLite** + in-memory cache + in-memory event pipeline, so nothing else needs to be installed or running.

```bash
dotnet run --project src/UrlShortener.Api --no-launch-profile --urls "http://localhost:5080"
```

Once it starts, open:
- **Web UI:** http://localhost:5080
- **Swagger (interactive API docs):** http://localhost:5080/swagger
- **Health check:** http://localhost:5080/healthz

Stop the service with `Ctrl+C`.

> The SQLite database file (`urlshortener.db`) is created automatically on first run.

---

## Option B — Run the full stack (PostgreSQL + Redis + Kafka)

Runs the containerized API wired to real infrastructure — this exercises the event-driven analytics pipeline (Kafka), the Redis cache, and the PostgreSQL relational store end-to-end.

```bash
docker compose up --build
```

Once all containers report healthy, open:
- **Web UI / API:** http://localhost:8080  (e.g. http://localhost:8080/healthz)

Tear everything down (including volumes) when finished:
```bash
docker compose down -v
```

> First run pulls base images and builds the app in a container, so it can take a few minutes.

---

## Verify it works (curl)

With the service running (adjust the port: `5080` for Option A, `8080` for Option B):

```bash
# 1) Create a short link (custom alias optional, expiresAt optional)
curl -X POST http://localhost:5080/api/urls \
  -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com","alias":"demo"}'
```

```bash
# 2) Follow the redirect (expect HTTP 302 -> Location: https://www.example.com/)
curl -i http://localhost:5080/demo
```

```bash
# 3) View click analytics for the code
curl http://localhost:5080/api/urls/demo/stats
```

Expected: step 1 returns `201` with a JSON body (note the `code` and `shortUrl`), step 2 returns `302`, step 3 returns click totals and per-day buckets.

> Auto-generated codes are intentionally **non-sequential** (e.g. `1GAJQWlAa`) — see [ADR-0007](adr/0007-short-code-strategy.md).

---

## Option C — Run the prebuilt executable (no .NET install needed)

Self-contained, single-file executables are produced under `dist/<platform>/` by
`scripts/build-executables.sh`. Each bundles the .NET runtime, so the target machine needs **nothing
installed**. It runs with zero infrastructure (SQLite + in-memory cache/events) and **auto-opens your
browser** to the UI.

| Platform | File to run |
|----------|-------------|
| macOS (Apple Silicon) | `dist/osx-arm64/UrlShortener` |
| macOS (Intel) | `dist/osx-x64/UrlShortener` |
| Windows x64 | `dist/win-x64/UrlShortener.exe` |
| Linux x64 | `dist/linux-x64/UrlShortener` |

It listens on **http://localhost:5080** (or automatically picks a free port if 5080 is busy) and opens the browser to the actual address.

**macOS** (Terminal):
```bash
./UrlShortener
```
The binary is unsigned, so Gatekeeper may block it on first launch. Either right-click → **Open**, or:
```bash
xattr -d com.apple.quarantine ./UrlShortener   # clear the quarantine flag, then run it
```

**Windows:** double-click `UrlShortener.exe` (or run it from a terminal). SmartScreen may warn about an
unknown publisher — choose **More info → Run anyway** (expected for an unsigned prototype binary).

**Linux:**
```bash
chmod +x ./UrlShortener && ./UrlShortener
```

The database is stored in a per-user folder (`~/.local/share/UrlShortener/` or the OS equivalent), so it
persists across runs and works regardless of where you launch the binary.

To rebuild the executables:
```bash
./scripts/build-executables.sh
```

## Option D — Portable Docker image (runs on any platform)

A Docker image runs the app as a Linux container on **any machine with Docker** (macOS, Windows, Linux).
It runs **standalone** — SQLite + in-memory cache/events inside the container, no other services — on
port **8080**.

### Build & run locally
```bash
docker build -t url-shortener:latest .
docker run -p 8080:8080 url-shortener:latest
```
Open http://localhost:8080. Add `-v urlshort-data:/data` to persist the database across runs.

### Multi-architecture image (amd64 + arm64)
One tag that runs natively on Intel and ARM hosts. Multi-arch manifests must live in a registry:
```bash
./scripts/build-image.sh multiarch <registry>/url-shortener:latest   # buildx build + push
# then on any host:
docker run -p 8080:8080 <registry>/url-shortener:latest
```

### Portable image tarball (no registry)
Save the image to a file, copy it to another machine (same CPU architecture), and load it:
```bash
./scripts/build-image.sh tarball                 # writes dist/url-shortener-image-<arch>.tar.gz
# on the other machine:
docker load -i url-shortener-image-<arch>.tar.gz
docker run -p 8080:8080 url-shortener:latest
```

> Helper script: `scripts/build-image.sh` (modes: `local`, `tarball`, `multiarch REF`).
> The full Postgres + Redis + Kafka stack is a separate concern — see **Option B** (`docker compose`).

## Run the tests

```bash
dotnet test
```

Expected result: **48 tests passing** (unit + integration). The same build + format gate + tests run in CI (`.github/workflows/ci.yml`).

Optional style gate (also enforced in CI):
```bash
dotnet format --verify-no-changes
```

---

## API reference

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/urls` | Create a short link. Body: `url` (required), `alias` (optional), `expiresAt` (optional ISO-8601) |
| `GET`  | `/{code}` | Redirect (`302`); `410` if expired; `404` if unknown |
| `GET`  | `/api/urls/{code}` | Link metadata |
| `GET`  | `/api/urls/{code}/stats` | Click analytics (total, last-clicked, per-day) |
| `GET`  | `/healthz` | Health check |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dotnet: command not found` | Install the .NET 10 SDK (see Prerequisites). |
| Port already in use / unexpected `500`s | A previous run may still be listening. Stop it: `pkill -f UrlShortener.Api`, or run on another port via `--urls`. |
| `docker compose` not found | Install Docker Desktop, or the Compose plugin; ensure the Docker daemon is running (`colima start` if using Colima). |
| Option B build is slow the first time | Expected — it pulls base images and compiles in a container. Subsequent runs are cached. |

---

## More documentation
- [README](../README.md) — project overview
- [Architecture](architecture.md) · [Setup details](setup.md) · [Testing](testing.md) · [Limitations & trade-offs](limitations.md)
- [Three scenarios](scenarios.md) · [Decision log](DECISIONS.md) · [ADRs](adr/) · [Final summary](engineering-summary.md)
