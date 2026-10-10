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

## Risk mitigation (documented, deferred)
Keep the sequential **storage** key but pass it through a reversible, keyed **Feistel permutation** (or Hashids) before Base62 encoding. This preserves zero-collision + O(1) while making emitted codes appear random and non-enumerable. Recorded as the first hardening step if this moved toward production. Tracked in `docs/limitations.md`.
