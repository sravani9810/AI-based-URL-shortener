# ADR-0007: Short-code generation — sequential ID encoded as Base62

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
Every short URL needs a compact, URL-safe code. The main strategies are random, sequential-encoded, or hash-based. This is both an engineering and a security decision.

## Decision
Generate codes by taking the **monotonic primary key** of the stored `ShortUrl` row and **encoding it in Base62** (`[0-9A-Za-z]`). The database guarantees uniqueness, so **no collision handling or retry loop is needed**; codes are the shortest possible for a given ID and generation is O(1).

## Alternatives considered
- **Random Base62 + collision check** — non-enumerable (better privacy), but needs a uniqueness check with bounded retry and a larger code for a safe keyspace. Rejected in favor of simplicity.
- **Hash of URL** — dedupes identical URLs but leaks equality of targets and still needs collision handling. Rejected.

## Consequences
- (+) Dead simple, collision-free, shortest codes, trivially testable (pure function `id ⇄ code`).
- (−) **Enumerable:** codes are guessable by incrementing — an actor can walk the keyspace and discover others' links. This is a real privacy/security limitation for a financial context.
- (−) Leaks approximate creation order/volume (business-intelligence leak).

## Risk mitigation — IMPLEMENTED (Phase 7)
The enumerability risk is now mitigated. The sequential **storage** key is kept, but passed through a reversible, keyed **Feistel permutation** over a 48-bit domain before Base62 encoding (`FeistelCodeGenerator` decorating `Base62CodeGenerator`). This preserves zero-collision + O(1) while making emitted codes appear random and non-enumerable (e.g. ids 1,2,3 → `1GAJQWlAa`, `jeVE49ZJ`, `sIo01FvB`). The permutation key comes from config (`ShortCode:Secret`). Verified by unit tests (round-trip, uniqueness across a range, non-sequential) and end-to-end.

**Concurrency note:** code uniqueness is guaranteed by the database (unique id → unique permuted code). A check-then-act race on a *custom alias* is caught at the unique index and translated to **HTTP 409** via `DuplicateCodeException` (see repository), rather than surfacing as a 500.
