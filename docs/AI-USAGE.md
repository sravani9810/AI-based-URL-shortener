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

> This table is appended to as each task is executed. Prompts that were materially important are kept in `docs/prompts/` (added as used).
