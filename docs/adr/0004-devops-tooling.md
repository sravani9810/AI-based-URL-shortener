# ADR-0004: Dockerfile + docker-compose + GitHub Actions CI

- **Status:** Accepted (2026-10-10)
- **Decision owner:** Engineer

## Context
The role is cloud-native (Docker, Kubernetes, CI/CD, GCP/AWS). A prototype should show a credible delivery pipeline without spending the whole budget on infra.

## Decision
- **Multi-stage Dockerfile** for a small runtime image.
- **docker-compose** wiring app + Kafka + Redis for a one-command full-stack run.
- **GitHub Actions CI**: restore → build → test → (lint/format check) on push/PR.

## Alternatives considered
- **Add Kubernetes/Helm manifests** — maximum credibility, but can't be exercised locally without a cluster and adds maintenance. Deferred; noted as a documented next step.
- **Dockerfile only** — too light a DevOps story for this role. Rejected.

## Consequences
- (+) `docker compose up` gives a full event-driven stack; CI enforces quality gates on every change.
- (−) No live k8s deploy demonstrated — documented as a limitation with the intended manifest set.
