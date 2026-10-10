# ADR-0001: .NET 8 ASP.NET Core Web API with a layered architecture

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
We need a stack that is runnable in 2–3 days yet reads as production-grade for a financial/trading platform, and that mirrors the target role (.NET + AI). The code must demonstrate modularity, testability, and clean design.

## Decision
Use **.NET 8 (LTS)**, **ASP.NET Core Web API** with **controllers**, organized in a **layered architecture**: `Api` (controllers, DTOs, filters) → `Application/Services` (business logic) → `Infrastructure` (EF Core, Redis, Kafka) → `Domain` (entities, interfaces). Dependencies point inward; infrastructure is injected via DI.

## Alternatives considered
- **Minimal APIs** — more modern and terse, but weaker structured/enterprise signal and awkward for layered testing at this size. Rejected.
- **Clean/vertical-slice + MediatR/CQRS** — impressive, but risks over-engineering a prototype and spending the time budget on plumbing. Rejected for now; noted as a future evolution.

## Consequences
- (+) Clear separation enables unit testing of services without the web host.
- (+) Familiar to .NET reviewers; easy to navigate.
- (−) Slightly more boilerplate than minimal APIs.
- Mitigation: keep layers thin; no premature abstraction.
