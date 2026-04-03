# Platform Diagnostics And Advanced Health Checks

## Purpose

This document defines the intended purpose, scope, and evolution path for the next-stage platform diagnostics capability in this repository.

It is not an implementation document yet. It is the design baseline for the future Platform Management Portal and for runtime operator diagnostics.

## Why This Exists

The repository already exposes basic health endpoints such as:

- `/api/v1/health`
- `/api/v1/ready`

Those endpoints are useful for liveness and readiness probes, but they are not enough for operator workflows such as:

- diagnosing dependency failures
- validating internal service connectivity
- checking runtime contract availability
- understanding degraded states
- running controlled advanced tests with elevated permissions

The next platform stage needs a richer, operator-facing diagnostic surface.

## Design Goals

- preserve fast, stable, machine-oriented health endpoints for probes
- add deeper operator diagnostics without overloading basic health endpoints
- support a future Platform Management Portal with authenticated and auditable advanced checks
- treat runtime OpenAPI availability and contract verification as part of platform diagnostics
- separate read-only diagnostics from privileged control actions

## Layered Model

### 1. Basic Runtime Health

Audience:

- load balancers
- orchestrators
- monitoring probes

Purpose:

- answer whether the service is alive
- answer whether the service is ready to accept traffic

Characteristics:

- fast
- low-cost
- low-detail
- safe for broad automated access

Examples:

- `GET /api/v1/health`
- `GET /api/v1/ready`

### 2. Operator Diagnostics

Audience:

- platform operators
- support engineers
- incident responders
- future Platform Management Portal users with elevated read permissions

Purpose:

- inspect service and dependency state in more depth
- run advanced health checks
- surface degraded conditions and recommended next steps
- verify runtime contract availability

Characteristics:

- authenticated
- more detailed
- may be slower than basic health probes
- should be audited

Examples:

- dependency connectivity checks
- authentication-chain checks
- message transport checks
- database reachability checks
- outbox and retry backlog visibility
- runtime OpenAPI availability and parseability checks

### 3. Platform Control Actions

Audience:

- a smaller set of platform administrators

Purpose:

- trigger controlled operational actions beyond passive diagnostics

Characteristics:

- highest privilege
- strongest audit requirements
- may include side effects
- should usually be rate-limited, guarded, or asynchronous

Examples:

- rerun a diagnostic suite
- trigger a contract export
- request a dependency re-check
- trigger a safe operational dry-run workflow

## Recommended API Boundary

Do not overload basic health endpoints with operator diagnostics.

Recommended separation:

- keep `/api/v1/health` and `/api/v1/ready` minimal
- introduce operator-focused endpoints under a system or platform namespace

Example direction:

- `GET /api/v1/system/info`
- `GET /api/v1/system/health-summary`
- `GET /api/v1/system/diagnostics`
- `POST /api/v1/system/diagnostics/run`
- `GET /api/v1/system/contracts`
- `POST /api/v1/system/contracts/verify`

The exact routes can evolve, but the boundary should stay explicit.

## Example Diagnostic Categories

### Service Diagnostics

- process is running
- service configuration is present
- background workers are active
- version and deployment metadata are available

### Dependency Diagnostics

- database connectivity
- RabbitMQ or transport availability
- downstream service authentication
- DNS and base URL reachability

### Workflow Diagnostics

- pending review queue size
- outbox backlog size
- retry queue state
- recent failed operation summary

### Contract Diagnostics

- runtime OpenAPI endpoint is reachable
- runtime OpenAPI document is parseable
- critical documented paths are present
- critical schemas are still valid
- runtime contract differs from last baseline

## Relationship To OpenAPI Source Of Truth

This future diagnostics layer should work well with a runtime OpenAPI source-of-truth approach.

Recommended model:

- the real service exposes runtime OpenAPI
- contract tests verify that runtime contract during test runs
- platform diagnostics can also verify runtime contract availability in deployed environments

That means contract verification becomes both:

- a test-stage concern
- a runtime operator concern

## Security Model

The platform should distinguish at least three levels of access.

### Probe

- can call only basic health endpoints

### Operator

- can view advanced diagnostics
- cannot run privileged or side-effecting control actions

### Platform Admin

- can trigger controlled advanced checks
- can access sensitive operational views and control-plane actions

## Audit Requirements

Every advanced diagnostic or control action should record:

- who triggered it
- when it was triggered
- which target service or dependency was checked
- which diagnostic category ran
- whether it succeeded, degraded, or failed

This is especially important if future diagnostics include any active or potentially expensive operations.

## Performance And Safety Principles

- basic health endpoints must stay fast and cheap
- expensive diagnostics should not run on every probe request
- heavy diagnostics should support asynchronous execution where needed
- diagnostics must redact or omit sensitive values such as raw secrets, internal keys, and connection strings
- runtime contract checks should not block basic liveness

## Suggested Phased Rollout

### Phase 1

- document the layered model
- keep existing health endpoints unchanged
- define the operator-diagnostics surface conceptually

### Phase 2

- expose runtime OpenAPI in testing or contract-specific environments
- introduce runtime contract verification tests
- add operator-facing diagnostic DTOs and route design

### Phase 3

- implement service-level diagnostics
- aggregate them in Gateway or a dedicated platform surface
- add authentication, authorization, and auditing

### Phase 4

- power a Platform Management Portal with these diagnostics
- add controlled advanced tests and historical diagnostic views

## Open Questions

- should diagnostics live in Gateway, a dedicated platform service, or both
- which runtime contract surfaces should be treated as public, internal, or operator-only
- which advanced diagnostics are safe to run synchronously
- how much contract drift history should be retained

## Current Recommendation

For the next stage, the most practical starting point is:

1. keep `health` and `ready` simple
2. treat advanced diagnostics as a separate operator surface
3. align runtime OpenAPI verification with that operator surface
4. use this document as the design baseline before implementation starts

Related API draft:

- `docs/37-platform-diagnostics-api-draft.md`
