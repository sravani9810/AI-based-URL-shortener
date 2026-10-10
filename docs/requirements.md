# Requirement Understanding & Task Decomposition

> Deliverable for Core Requirements #1 (Requirement Understanding) and #2 (Task Decomposition).
> This document is maintained by the engineer; AI assists within tasks.

## 1. Interpreting the intent

The assignment asks for a **URL shortener service built with AI-assisted engineering**, graded primarily on **engineering judgment and AI-assisted execution**, not on product novelty. The product is the vehicle; the discipline is the point.

Normalized engineering problem:

> Build a runnable, production-shaped URL shortener service that exposes APIs to create short links and redirect them, tracks click analytics without slowing redirects, protects itself against abuse, and is documented and tested to a standard an engineer would defend in production review — while demonstrating a disciplined, traceable AI-assisted workflow.

## 2. Ambiguities identified (and how we resolved them)

| # | Ambiguity | Resolution | Owner decision |
|---|-----------|------------|----------------|
| A1 | "AI-based URL shortener" — is AI a product feature or the engineering assistant? | PDF treats AI as the engineering accelerator. No AI feature in the product. | ADR-0005 |
| A2 | What counts as "runnable end-to-end"? | Must run with minimal setup; infra deps (Kafka/Redis) are optional with graceful fallbacks. | ADR-0002, ADR-0003 |
| A3 | Scale target? Not specified. | Prototype scale, but architected so the hot path (redirect) and analytics scale independently. | ADR-0001, ADR-0002 |
| A4 | Analytics depth? "Analytics" is vague. | Start with count + timestamp + referrer + user-agent; this is deliberately the **ambiguous scenario**. | scenarios.md |

## 3. Non-functional requirements (derived)

- **Performance:** redirect is the hot path — must not block on analytics or external infra.
- **Reliability/resiliency:** service stays up if Kafka/Redis are down (degraded, not failed).
- **Security:** validate/normalize input URLs; rate-limit the public create endpoint; no open redirect to dangerous schemes.
- **Observability:** health checks + structured logging.
- **Maintainability/testability:** layered, dependency-injected, unit + integration tested.

## 4. Task decomposition (with dependencies & sequencing)

```
T0  Project scaffold (solution, projects, DI, config)                 [no deps]
T1  Domain + data layer: ShortUrl entity, EF Core, SQLite             [T0]
T2  Short-code generation (Base62) + collision handling              [T1]
T3  Create endpoint: POST /api/urls (validation, custom alias)       [T1,T2]
T4  Redirect endpoint: GET /{code} (lookup, expiry check, 302/404)   [T1]
T5  Caching layer (Redis, with in-memory fallback)                   [T4]
T6  Event pipeline abstraction (IEventPublisher) + in-proc fallback  [T0]
T7  Click analytics: publish event on redirect, consumer persists    [T4,T6]
T8  Analytics endpoint: GET /api/urls/{code}/stats                   [T7]
T9  Rate limiting + abuse protection on create                       [T3]
T10 Expiration support (TTL on links, expiry on redirect)            [T3,T4]
T11 Cross-cutting: health checks, logging, error handling            [T0]
T12 Tests: unit (code-gen, validation, services) + integration      [T2..T10]
T13 Containerization: Dockerfile + docker-compose (app+Kafka+Redis)  [all]
T14 CI pipeline: build + test + lint                                 [T12]
T15 Docs: architecture, setup, testing, scenarios, final summary     [all]
```

Critical path: **T0 → T1 → T2 → T3/T4 → T7 → T8**. Everything else hangs off that spine.

## 5. Scope decisions (confirmed by engineer)

**In scope (product capabilities):**
- Create short URL + redirect (core)
- Custom aliases + expiration
- Click analytics (event-driven)
- Rate limiting + abuse protection

**Out of scope (documented limitations):** user accounts/auth, UI/front-end, link editing/deletion UI, multi-region, full phishing-detection (no product AI). See `docs/limitations.md` (added later).
