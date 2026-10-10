# ADR-0008: Click analytics scope & rate limiting (ambiguous-requirement resolution)

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
The requirement "add analytics" is deliberately underspecified (the assignment's *ambiguous* scenario). Left unresolved it could mean anything from a single counter to a full BI pipeline. The engineer normalized it into concrete, defensible choices.

## Ambiguity → decision

| Open question | Resolution | Rationale |
|---------------|-----------|-----------|
| What do we measure? | Per-click rows: timestamp, referrer, user-agent | Enables counts **and** breakdowns/history without over-building |
| Store IP / identity? | **No raw IP**, no identity | PII minimization for a financial platform; documented retention limitation |
| Real-time or batch? | Near-real-time via **events** | Redirect must stay fast; analytics decoupled from the hot path |
| May it slow redirects? | **No** — publish-and-return | 302 responds after resolve; a background consumer persists clicks (ADR-0002) |
| What does the API expose? | `GET /api/urls/{code}/stats`: total, last-clicked, **per-day buckets** | Useful summary + chart-ready series |
| How do we protect the write surface? | **Token-bucket rate limit per client IP** on create (20/min, 429) | Abuse protection; allows controlled bursts then steady refill |

## Implementation notes
- `UrlClicked` is published via `IEventPublisher` (Kafka producer, or in-memory channel fallback); a background consumer persists `ClickEvent` rows. Verified end-to-end against a real Kafka container (messages observed on `url-clicks`; stats reflected the counts) and via the in-memory path in tests.
- Stats aggregation is computed in-memory from click rows — fine at prototype scale; a production system would pre-aggregate or use a time-series store (see limitations).
- Rate limiting uses ASP.NET Core's built-in `TokenBucketRateLimiter`, partitioned by `RemoteIpAddress`.

## Consequences
- (+) Demonstrates event-driven, high-throughput-shaped design directly from the role's JD.
- (+) Privacy-conscious; redirect latency unaffected by analytics or limiter.
- (−) Analytics are eventually consistent and aggregation is naive at scale — both documented.
- (−) IP-based limiting is coarse behind proxies/NAT; a real deployment keys on API identity.
