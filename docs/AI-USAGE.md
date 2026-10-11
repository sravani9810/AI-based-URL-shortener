# AI Usage & Traceability Log

> Deliverable for Core Requirement #4 (AI-Assisted Execution) — maintain traceability of what AI
> **generated**, what the engineer **edited**, and what was **rejected**, with rationale.
> Principle: AI assists within tasks; the engineer owns correctness, maintainability, production readiness.

## Legend
- **G** = AI-generated and accepted largely as-is (engineer reviewed)
- **E** = AI-generated, then edited by engineer (what/why noted)
- **R** = AI-proposed, rejected by engineer (why noted)

## Workflow discipline applied
Every AI-assisted task is defined with: **intent · constraints · acceptance criteria · technical context**, then iteratively refined. High-impact changes require explicit engineer sign-off before merge.

## Log

| Date | Task | Type | Summary | Engineer action / rationale |
|------|------|------|---------|-----------------------------|
| 2026-10-10 | Requirement analysis & decomposition | E | AI drafted `requirements.md` decomposition and ambiguity table | Engineer reviewed sequencing & scope; owns the normalized problem statement |
| 2026-10-10 | Architecture decisions (D1–D7) | E | AI presented options + trade-offs for each decision | Engineer made every call; recorded as ADRs |
| 2026-10-10 | Phase 1 scaffold (solution, 4 projects, DI) | G | AI generated solution/project structure & references via dotnet CLI | Engineer chose layout (D13); reviewed dependency direction |
| 2026-10-10 | Base62 code generator + unit tests | G | AI generated encoder/decoder and round-trip tests | Engineer verified Base62 edge cases (0, negative, long.MaxValue) |
| 2026-10-10 | UrlService (validation, alias, codes) | E | AI generated service; engineer added reserved-code list & http/https-only rule | Engineer owns URL/alias validation policy (security) |
| 2026-10-10 | Controllers + exception handler | G | AI generated create/redirect controllers + ProblemDetails handler | Engineer confirmed 302 (not 301) for analytics; reviewed status mapping |
| 2026-10-10 | EF Core model + migration | E | AI generated DbContext, repository, InitialCreate migration | Engineer chose filtered unique index for transient-null code |
| 2026-10-10 | Integration tests (WebApplicationFactory) | G | AI generated end-to-end create→redirect tests | Engineer verified all 28 tests pass + manual curl run |
| 2026-10-10 | Phase 2: ICacheStore + in-memory/Redis impls | E | AI generated cache-aside abstraction & both stores | Engineer chose graceful-degradation policy (cache failure = miss, never fail redirect) |
| 2026-10-10 | Phase 2: Redis overload bug | E | AI's `StringSetAsync(key,value,ttl)` hit an ambiguous overload | Engineer fixed by passing `When.Always` explicitly |
| 2026-10-10 | Phase 2: expiration + 410 handling | E | AI generated ExpiresAt, ResolveResult, validation | Engineer owns 410-vs-404 semantics and future-expiry rule |
| 2026-10-10 | Phase 2: deterministic time in tests | E | AI wired FakeTimeProvider into the test host | Engineer required no real-time delays in tests (flakiness) |
| 2026-10-10 | Phase 2: Redis verified via Colima | G | AI ran Redis container, confirmed key+TTL and DB fallback | Engineer confirmed resiliency manually (Redis-down redirect) |
| 2026-10-10 | Phase 3: ambiguity resolution ("add analytics") | E | AI framed open questions as concrete options | Engineer decided data model, stats shape, rate-limit policy (ADR-0008) |
| 2026-10-10 | Phase 3: IEventPublisher + in-memory/Kafka | G | AI generated event bus, consumers, producer | Engineer required best-effort publish off the hot path; no raw IP |
| 2026-10-10 | Phase 3: analytics repo + stats endpoint | E | AI generated aggregation + GET /stats | Engineer accepted in-memory aggregation as a documented prototype trade-off |
| 2026-10-10 | Phase 3: token-bucket rate limiting | G | AI wired ASP.NET rate limiter + 429 test | Engineer chose token-bucket per IP and limits |
| 2026-10-10 | Phase 3: real Kafka verification | G | AI ran Kafka (KRaft) container, confirmed topic + messages + stats | Engineer verified messages carry no IP and counts match |
| 2026-10-10 | Phase 4: multi-stage Dockerfile + .dockerignore | G | AI generated layer-cached build + slim runtime image | Engineer chose Debian base for Kafka native deps |
| 2026-10-10 | Phase 4: docker-compose (PG+Redis+Kafka+API) | E | AI generated compose with healthchecks + depends_on | Engineer chose Postgres and the provider-swap approach |
| 2026-10-10 | Phase 4: config-selected EF provider | E | AI added Npgsql + provider switch + EnsureCreated/Migrate split | Engineer accepted EnsureCreated-for-PG as documented trade-off |
| 2026-10-10 | Phase 4: GitHub Actions CI + format gate | G | AI generated restore/format/build/test workflow | Engineer required format verify as a quality gate |
| 2026-10-10 | Phase 5: architecture/setup/testing/limitations/summary docs | E | AI drafted deliverable docs | Engineer verified claims against actual runs; owns summary |
| 2026-10-10 | Phase 5: Mermaid diagrams + PNG render | G | AI authored diagrams and rendered PNGs | Engineer confirmed diagrams match the implemented flow |
| 2026-10-10 | Phase 5: README as entry point | E | AI generated full README | Engineer confirmed commands/examples run as written |
| 2026-10-10 | Phase 6: minimal static UI (wwwroot) | G | AI generated index.html (shorten + stats) + static-file wiring | Engineer verified UI renders and the shorten flow works in-browser |
| 2026-10-10 | Phase 7: Feistel non-enumerable codes | E | AI implemented keyed Feistel decorator + tests | Engineer prompted the hardening after spotting enumerability; verified codes random |
| 2026-10-10 | Phase 7: alias-race → 409 translation | E | AI added DuplicateCodeException + provider-aware unique-violation mapping | Engineer identified the TOCTOU 500 gap; verified 409 end-to-end |
| 2026-10-10 | Phase 7: namespace-shadowing bug | E | AI's `Encoding.UTF8` resolved to local namespace (build break) | Engineer-side fix: fully-qualified `System.Text.Encoding` |

> This table is appended to as each task is executed. Prompts that were materially important are kept in `docs/prompts/` (added as used).
