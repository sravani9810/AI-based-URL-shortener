# Limitations, Risks & Trade-offs

> Deliverable: limitations and trade-offs. Owning these explicitly is part of the engineering judgment.

## Known limitations (prototype scope)
| Area | Limitation | Rationale / mitigation path |
|------|-----------|------------------------------|
| Short codes | **Enumerable** (sequential id → Base62) — codes are guessable by incrementing | Deliberate (ADR-0007). Mitigation: keyed Feistel/Hashids permutation keeps codes collision-free but non-sequential. |
| AuthN/Z | **No authentication** — all endpoints are open | Out of scope; rate limiting partially protects create. Production: API keys/OAuth + per-identity limits. |
| Analytics scale | Stats aggregated **in-memory** from click rows per request | Fine at prototype scale; production would pre-aggregate counters or use a time-series store. |
| Analytics consistency | Click counts are **eventually consistent** (async consumer) | Accepted trade-off to keep the redirect hot path fast (ADR-0002). |
| Postgres migrations | Compose uses `EnsureCreated()` for Postgres, not migrations | Avoids dual migration assemblies in a prototype (ADR-0003). Production: dedicated Postgres migrations assembly. |
| Rate limiting | Keyed by **client IP** — coarse behind proxies/NAT, per-instance state | Prototype-appropriate; production keys on API identity and uses a distributed limiter. |
| Code/alias namespace | Auto codes and custom aliases share one unique column | Unique index guarantees integrity; a rare auto/alias clash is an accepted edge case. |
| Link management | No update/delete endpoints; no UI | Out of scope; create + redirect + stats cover the core. |
| Security headers/TLS | TLS expected to terminate at ingress; no HSTS/security headers added | Standard for a service behind a gateway; documented, not implemented here. |
| Open-redirect surface | Targets limited to http/https; no allow/deny-list or malware screening | Validation blocks dangerous schemes; richer screening is future work. |

## Risks & failure scenarios (and how they're handled)
| Scenario | Behavior | Guardrail |
|----------|----------|-----------|
| Redis down | Redirect falls back to DB | `ICacheStore` failures → cache miss, never throw (verified) |
| Kafka down | Redirect still succeeds; click may be dropped | Publish is best-effort off the hot path (ADR-0002) |
| DB unavailable | Create/redirect fail (expected) | 500 via ProblemDetails; health check surfaces it |
| Abuse / flooding create | Excess requests rejected | Token-bucket rate limiter → 429 |
| Malformed/expired input | Clear 4xx | Validation (400), conflict (409), expiry (410) |
| Corrupt cache entry | Treated as a miss | Deserialize guarded; falls back to DB |

## Trade-offs summary
- **SQLite-first** for instant runnability vs. production RDBMS fidelity → mitigated by provider-swap to Postgres in compose.
- **In-memory fallbacks** (cache/events) for zero-infra runs vs. always-on realism → same code paths run against real Redis/Kafka.
- **Simplicity over completeness** (no auth/UI) to keep focus on the graded core: AI-assisted execution, decomposition, resiliency, and defensible decisions.
