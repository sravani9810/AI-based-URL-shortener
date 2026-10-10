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

> This table is appended to as each task is executed. Prompts that were materially important are kept in `docs/prompts/` (added as used).
