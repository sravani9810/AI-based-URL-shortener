# ADR-0006: Product feature scope

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
We need a feature set that is realistic and defensible, demonstrates range, and each item maps to a target-role skill — without overreaching a 2–3 day prototype.

## Decision
Ship these capabilities:

1. **Create short URL + redirect** (core) — the non-negotiable value: `POST /api/urls` → short code; `GET /{code}` → 302.
2. **Custom aliases + expiration** — user-chosen codes with collision handling; optional TTL/expiry enforced on redirect. Shows validation & edge-case handling.
3. **Click analytics** — count + timestamp + referrer + user-agent via the event pipeline; `GET /api/urls/{code}/stats`. Where Kafka earns its place.
4. **Rate limiting + abuse protection** — per-client limits on `POST /api/urls`. Resiliency/security.

## Alternatives / deferred
- User accounts & auth, link editing/deletion, QR codes, UI front-end, bulk import — **out of scope**, documented in limitations.

## Consequences
- (+) Covers API design, validation, event-driven analytics, caching, and resiliency — a broad but coherent demo.
- (−) No auth means endpoints are open; rate limiting partially compensates and the limitation is documented.
