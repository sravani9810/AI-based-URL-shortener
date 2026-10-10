# ADR-0002: Event-driven click analytics via Kafka, behind an abstraction

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
Redirect is the latency-critical hot path. Writing analytics synchronously on every redirect couples a user-facing request to a write and caps throughput. The target role emphasizes **event-driven architecture with Kafka** for high-volume transaction processing — a URL shortener's click stream is a natural fit.

## Decision
On redirect, publish a lightweight **`UrlClicked` event** to an `IEventPublisher`. The default implementation publishes to **Kafka**; a **background consumer** (`IHostedService`) persists/aggregates clicks. The redirect responds immediately after resolving the target, never waiting on analytics.

The publisher is an **abstraction** with a **in-memory channel fallback** (`System.Threading.Channels`) used when Kafka is not configured/available, so the service runs end-to-end **without Docker/Kafka**.

## Alternatives considered
- **Synchronous DB write on redirect** — simplest, but couples hot path to writes and discards the #1 role differentiator. Rejected.
- **In-process channel only (no real Kafka)** — zero infra but doesn't actually demonstrate Kafka. Kept as the *fallback*, not the primary.

## Consequences
- (+) Redirect stays fast; analytics scale independently; demonstrates real event-driven design.
- (+) Resilient: analytics degrade (buffer/fallback) instead of failing the redirect if Kafka is down.
- (−) Eventual consistency for analytics (counts lag slightly) — acceptable and documented.
- (−) More moving parts (broker, consumer) — mitigated by the fallback and docker-compose.
