# Final Engineering Summary

> Deliverable #8: plan/rationale, artifacts, risks/trade-offs/validation, assumptions, and limitations.

## 1. Plan & rationale
The assignment is graded on **AI-assisted engineering judgment**, not product novelty. The plan was therefore built around *defensible, engineer-owned decisions*, executed in reviewable phases, with AI as an accelerator and the engineer approving every output.

The stack was deliberately aligned to the target role (**.NET + AI, event-driven, cloud-native, financial platform**) so every choice is justifiable as "what I'd do on the job": ASP.NET Core, event-driven analytics via Kafka, relational + NoSQL stores, Docker/CI, resiliency and rate limiting.

Work proceeded in phases, each its own commit:
| Phase | Scenario | Outcome |
|------|----------|---------|
| 0 | — | Requirements, decomposition, decisions, ADRs |
| 1 | **Greenfield** | Core create/redirect + custom aliases (+ tests) |
| 2 | **Brownfield** | Redis cache-aside + expiration on the existing redirect path |
| 3 | **Ambiguous** | Event-driven click analytics + rate limiting |
| 4 | — | Dockerfile, docker-compose (PG+Redis+Kafka), GitHub Actions CI |
| 5 | — | Architecture, setup, testing, limitations, this summary |

## 2. How AI was used (and owned)
Defined each task with **intent, constraints, acceptance criteria, technical context**; iterated; and maintained full traceability in [`AI-USAGE.md`](AI-USAGE.md) (generated / edited / rejected, with rationale). The engineer made every architectural decision ([`DECISIONS.md`](DECISIONS.md), [`adr/`](adr/)), caught and corrected AI output (e.g. the Kafka `StringSetAsync` overload bug; requiring no-raw-IP; requiring deterministic time in tests), and validated results by building, testing, and exercising real infrastructure.

## 3. Artifacts
- **Runnable service** — 4-project layered .NET 10 solution + tests.
- **APIs** — `POST /api/urls`, `GET /{code}` (302/410/404), `GET /api/urls/{code}/stats`, `/healthz`, Swagger.
- **Docs** — [architecture](architecture.md) (with diagrams), [setup](setup.md), [testing](testing.md), [limitations](limitations.md), [scenarios](scenarios.md), ADRs 0001–0008, decision log, AI-usage log.
- **DevOps** — multi-stage Dockerfile, docker-compose full stack, CI pipeline with a format gate.

## 4. Validation
- **38 automated tests** (unit + integration), all passing; CI enforces build + format + test.
- **Real-infra verification:** Redis cache + TTL and **Redis-down fallback**; Kafka messages on `url-clicks` reflected in stats; full **docker-compose on PostgreSQL** with click rows confirmed directly in the DB.
- Determinism via `FakeTimeProvider` and in-memory infra implementations.

## 5. Risks, trade-offs, limitations
Captured in full in [`limitations.md`](limitations.md). Headlines: enumerable codes (deliberate, mitigation documented), no auth (rate-limiting partially compensates), in-memory analytics aggregation and eventual consistency (prototype scale), `EnsureCreated` for Postgres in compose (vs. a production migrations assembly).

## 6. Assumptions
- Prototype scale; TLS/security headers handled at an ingress/gateway.
- Analytics may be eventually consistent and must never slow redirects.
- "AI-based" refers to the engineering process, not a product AI feature (ADR-0005).
- Graders value reviewable incremental history and defensible decisions over feature count.

## 7. If this went to production (next steps)
Non-sequential codes (Feistel/Hashids) · auth + per-identity limits · pre-aggregated/time-series analytics · dedicated Postgres migrations · distributed rate limiting · observability (metrics/tracing) · k8s manifests + deployment pipeline.

## 8. Core principles demonstrated
Modular (layered, DI) · testable (38 tests, fakes, deterministic time) · reliable (graceful degradation, health checks) · secure-minded (input validation, rate limiting, PII minimization) · scalable-shaped (event-driven hot path, cache, provider swap) · safe change management (phased commits, ADRs, CI gates).
