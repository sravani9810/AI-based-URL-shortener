# Decision Log

Running, chronological log of every significant decision. Each major one also has a full ADR in `docs/adr/`. The engineer owns and approves every decision; AI proposed options and rationale.

| Date | ID | Decision | Owner call | ADR |
|------|----|----------|-----------|-----|
| 2026-10-10 | D1 | Build in **.NET 8, ASP.NET Core Web API, Controllers + layered architecture** | Chosen over Minimal APIs / vertical-slice — enterprise/financial feel, modular & testable | [ADR-0001](adr/0001-dotnet-webapi-layered.md) |
| 2026-10-10 | D2 | **Event-driven analytics via Kafka**, behind `IEventPublisher`, with in-memory fallback | Chosen over sync writes / in-process-only — showcases event-driven skill, stays runnable without Docker | [ADR-0002](adr/0002-event-driven-analytics.md) |
| 2026-10-10 | D3 | **EF Core + SQLite** (relational) + **Redis** (cache/counters) | Covers relational + NoSQL; SQLite keeps it instantly runnable; Redis optional | [ADR-0003](adr/0003-data-layer.md) |
| 2026-10-10 | D4 | **Dockerfile + docker-compose + GitHub Actions CI** | Cloud-native story without k8s overhead for a prototype | [ADR-0004](adr/0004-devops-tooling.md) |
| 2026-10-10 | D5 | **No AI feature in the product**; AI = engineering assistant only | Faithful to PDF; lowest scope/risk; avoids API-key dependency | [ADR-0005](adr/0005-no-product-ai.md) |
| 2026-10-10 | D6 | **Feature scope**: core shorten+redirect, custom aliases+expiration, click analytics, rate limiting | Realistic, defensible, each maps to a JD skill | [ADR-0006](adr/0006-feature-scope.md) |
| 2026-10-10 | D7 | Work on branch **`feat/url-shortener`**; docs as ADRs + this log + AI-USAGE traceability | Standard, reviewable change management | — |
| 2026-10-10 | D8 | **Scenario mapping**: greenfield=core+aliases, brownfield=caching+expiry, ambiguous=analytics | Maps required scenarios onto real features | [scenarios.md](scenarios.md) |
| 2026-10-10 | D9 | **Short code = sequential ID → Base62** (no collision handling) | Chosen over random/hash for simplicity; enumerability risk accepted & mitigation documented | [ADR-0007](adr/0007-short-code-strategy.md) |
| 2026-10-10 | D10 | Solution/namespace name = **`UrlShortener`** | Clean, neutral | — |
| 2026-10-10 | D11 | **Phase-wise commits, committed by the engineer** (not AI) | Separate reviewable commit per phase; engineer owns the history | [scenarios.md](scenarios.md) |

## How to read this
- **D#** = decision id. **ADR** = full context/alternatives/consequences.
- Decisions can be superseded; a superseded ADR is marked and links to its replacement.
