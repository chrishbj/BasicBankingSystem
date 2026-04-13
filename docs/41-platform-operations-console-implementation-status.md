# Platform Operations Console Implementation Status

## Purpose

This document records what has already been implemented for the `Platform Operations Console`, what remains intentionally simplified, and what should be pushed next.

It complements:

- [Platform Identity And Operations Architecture](./38-platform-identity-and-operations-architecture.md)
- [Platform Operations Console Detailed Design](./39-platform-operations-console-detailed-design.md)
- [Docker Desktop Run Guide](./12-docker-desktop-run.md)

## What Exists Today

The repository now includes a working first version of the platform console:

- frontend app: `src/Banking.PlatformOps`
- Docker image and Nginx proxy for local stack execution
- Gateway control-plane routes under `/api/platform/*`
- Docker Desktop entry point at `http://localhost:18089`

The current frontend includes these tabs:

1. `Overview`
2. `Services`
3. `Compatibility`
4. `Rollouts`
5. `Environments`
6. `Workflows`
7. `Diagnostics`
8. `Maintenance`
9. `Audit`

## Current Backend Capabilities

### Platform Read APIs

The `Gateway` currently exposes:

- `GET /api/platform/overview`
- `GET /api/platform/services`
- `GET /api/platform/compatibility`
- `GET /api/platform/rollouts`
- `GET /api/platform/environments`
- `GET /api/platform/workflows/deposits`
- `GET /api/platform/workflows/deposits/pending-review`
- `GET /api/platform/workflows/deposits/runtime`
- `GET /api/platform/diagnostics/correlations/{correlationId}`
- `GET /api/platform/audit`

### Controlled Maintenance APIs

The current demo also includes selected controlled actions for deposit support workflows:

- retry deposit compensation
- resolve deposit pending review
- requeue deposit outbox work

These actions already support the control-plane concept, but they are still demo-scoped rather than production-grade privileged operations.

## Current Design Wins

The implemented version already demonstrates several intended platform-design principles.

### 1. Separate Control Planes

`Banking.Web` remains the business operations surface.

`Banking.PlatformOps` is now a separate runtime support and diagnostics surface.

### 2. Productized Engineering Assets

The platform console reuses and reshapes existing engineering assets:

- runtime health and readiness checks
- OpenAPI contract availability
- workflow diagnostics support
- audit and support-oriented maintenance hooks

### 3. Boundary-Aware Compatibility

The `Compatibility` module now fetches runtime `openapi/v1.json` documents from downstream services and compares them to critical-path baselines.

The current payload includes:

- runtime title and version
- runtime path count
- expected critical path count
- missing critical path count
- missing critical path list
- parse or fetch errors

### 4. Docker-First Operability

The platform console is not just a design file.

It now runs inside the same Docker Desktop stack as the rest of the system, which makes local platform workflows demonstrable end to end.

## Known Simplifications

The current version is intentionally incomplete in several areas.

### Identity And Access

- local API-key based auth is still used for the demo
- no Azure Entra ID or external IAM integration yet
- no first-class support-access workflow yet
- no separate security administration console yet

### Platform Data

- rollout status is a modeled summary, not fed by a real deployment controller
- environment comparison is a snapshot shape, not a persisted diff system
- compatibility checks compare current runtime state only, not historical drift over time
- no live links to external logs, traces, dashboards, or incident tools yet

### Governance

- maintenance actions are controlled, but not yet protected by approval flows or step-up auth
- synthetic verification is still mostly a design direction rather than a dedicated module

## Recommended Next Steps

### Next

- persist compatibility snapshots and show drift history
- enrich `Environments` with version, config, and feature-flag diffs
- add service drill-through links for logs, traces, runbooks, and dashboards
- move rollout summary from health-derived heuristics toward explicit deployment metadata

### After That

- implement a dedicated `Support Access` module with temporary elevation and audit
- separate compatibility by surface type: public service, internal service, Gateway, BFF, and operator APIs
- add governed synthetic checks and run history
- introduce incident context objects and ticket references into platform audit records

### Longer Term

- align the IAM model with Azure-hosted multi-bank deployment reality
- add multi-environment aggregation beyond local and Docker
- separate platform operations and security administration more clearly as permissions mature

## Verification Notes

The current implementation has been validated with:

- `Gateway` integration tests
- `PlatformOps` frontend production build
- Docker Desktop stack rebuild and manual smoke checks

At the time of writing, the compatibility view successfully returns runtime OpenAPI comparison details for:

- `customer`
- `account`
- `deposit`
- `audit`
