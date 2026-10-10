# The Three Scenarios

> Deliverable: three scenarios — **greenfield, brownfield, ambiguous** — each showing
> **decomposition → execution → validation**. These are built in phases, each its own commit.

## Scenario 1 — Greenfield: core shortener from scratch
**Requirement (well-defined):** "Build a service to create short URLs and redirect them, including user-chosen custom aliases."

- **Decomposition:** solution scaffold → domain/entity → EF Core + SQLite → Base62 code gen → `POST /api/urls` (validation, optional custom alias) → `GET /{code}` redirect (302/404).
- **Execution:** Phase 1 (see `requirements.md` tasks T0–T4, plus custom alias).
- **Validation:** unit tests for Base62 and validation; integration test create→redirect round-trip; invalid URL and duplicate-alias cases.

## Scenario 2 — Brownfield: enhance the existing redirect path
**Requirement (enhancement/refactor on existing code):** "Redirects hit the database every time and links live forever. Add caching and link expiration without breaking existing behavior."

- **Codebase reasoning:** impacted modules = redirect controller, lookup service, repository, `DbContext`; data-flow change = introduce a cache-aside read and an expiry check.
- **Decomposition:** add `ICacheStore` (Redis + in-memory fallback) → cache-aside in the lookup service → add `ExpiresAt` to entity + migration → enforce expiry on redirect (410/404) → backfill tests.
- **Execution:** Phase 2 (tasks T5, T10).
- **Validation:** cache hit/miss tests; expired-link test; regression tests prove existing create→redirect still passes.

## Scenario 3 — Ambiguous: "add analytics"
**Requirement (deliberately underspecified):** "We want analytics on the links."

- **Ambiguity resolution (engineer-led):** What metrics? → clicks over time, referrer, user-agent. Real-time or batch? → near-real-time via events. Must it slow redirects? → no, hot path stays fast → **event-driven**. Retention/PII? → store coarse fields only, document retention as a limitation.
- **Decomposition:** `IEventPublisher` (Kafka + in-memory fallback) → publish `UrlClicked` on redirect → background consumer persists/aggregates → `GET /api/urls/{code}/stats` → add rate limiting so analytics/abuse surface is protected.
- **Execution:** Phase 3 (tasks T6–T9).
- **Validation:** publisher/consumer unit tests; stats endpoint integration test; redirect-latency unaffected when consumer is slow/down (resiliency).

## Phase → commit plan
| Phase | Scenario | Content | Commit (engineer) |
|------|----------|---------|-------------------|
| 0 | — | Docs: requirements, decisions, ADRs, scenarios | `docs: requirements, decisions, ADRs, scenarios` |
| 1 | Greenfield | Solution scaffold + core create/redirect + custom aliases + tests | `feat: greenfield core shortener (create + redirect + aliases)` |
| 2 | Brownfield | Redis cache-aside + expiration + regression tests | `feat: brownfield caching + link expiration` |
| 3 | Ambiguous | Event-driven analytics (Kafka + consumer) + rate limiting + tests | `feat: ambiguous analytics via event pipeline + rate limiting` |
| 4 | — | DevOps: Dockerfile, docker-compose, CI | `ci: containerization and GitHub Actions pipeline` |
| 5 | — | Final docs: architecture, setup, testing, limitations, summary | `docs: architecture, setup, testing, final summary` |
