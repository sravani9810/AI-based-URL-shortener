# ADR-0005: No AI feature in the product; AI is the engineering assistant

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
The repository is named `AI-based-URL-shortener`, which could imply an AI product feature. The assignment PDF, however, frames AI as the **engineering accelerator** ("AI assists the engineer within tasks; the engineer owns execution and quality"), not a product requirement. This is a genuine ambiguity (A1 in `requirements.md`).

## Decision
**No AI capability in the product.** AI is used only to accelerate engineering (implementation, tests, docs, review prep), with full traceability in `AI-USAGE.md`. The product is a classic, excellent URL shortener.

## Alternatives considered
- **AI phishing/malicious-URL check at shorten time** — strong security fit for finance, but adds scope, an external-model/API-key dependency, and a reliability surface. Rejected for this prototype; recorded as a high-value future enhancement.
- **AI slug suggestions / NL analytics summary** — demoable but weak fit and extra scope. Rejected.

## Consequences
- (+) Faithful to the graded intent; lowest risk; fully runnable with no API keys.
- (+) Keeps the spotlight on engineering discipline and AI-assisted *process*.
- (−) Doesn't literally match the repo name — explicitly reconciled here so the choice is defensible.
