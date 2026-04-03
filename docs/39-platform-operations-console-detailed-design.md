# Platform Operations Console Detailed Design

## Purpose

This document defines the detailed design for the future `Platform Management Console`.

It turns the platform architecture direction into concrete modules, data boundaries, permission tiers, and rollout phases.

It builds on:

- [Platform Identity And Operations Architecture](./38-platform-identity-and-operations-architecture.md)
- [Gateway And Customer BFF Design](./32-gateway-and-customer-bff-design.md)
- `docs/33-test-design-standards.md`
- `docs/34-testing-roadmap.md`
- `docs/35-test-design-findings-and-remedies.md`
- `docs/36-platform-diagnostics-and-advanced-health-checks.md`
- `docs/37-platform-diagnostics-api-draft.md`

## Design Goal

The console should act as the platform control plane for runtime support and governed operational diagnostics.

It should not become:

- a replacement for observability tools
- a replacement for developer scripts
- a mixed business-and-platform admin portal
- a place where contract boundaries become ambiguous

Its job is to present high-value, operator-ready information and controlled actions across environments.

## Primary Users

The initial personas should be:

- `PlatformOperator`
- `SupportEngineer`
- `SREOperator`
- `PlatformAdmin`
- `SecurityAdmin`

Optional later personas:

- `ReleaseManager`
- `TestEnvironmentAdministrator`

## Main Scope

### In Scope

- service health by environment
- deployed build and version visibility
- API and contract compatibility status
- rollout and canary status
- incident diagnostics
- support access controls
- environment and config comparison
- logs and traces entry points
- governed synthetic verification
- audit trail for privileged platform actions

### Out Of Scope

- day-to-day business customer servicing
- ordinary account operations
- branch workflow approvals
- customer-facing self-service
- deep raw telemetry storage
- arbitrary developer command execution

## Design Constraints Learned From Recent Testing Work

The console design should explicitly reflect recent repository decisions.

### 1. Boundaries Must Stay Visible

From the contract and test cleanup, one lesson is clear:

- public service APIs
- internal service APIs
- Gateway or BFF routes
- demo endpoints
- platform operator endpoints

must remain distinct surfaces.

The console may display them together in one user experience, but it must still label which surface each signal belongs to.

### 2. Failures Must Point Somewhere Useful

The platform should avoid vague "red dashboard" states.

Diagnostics should help operators answer:

- is this a service problem
- a dependency problem
- a workflow backlog problem
- a contract drift problem
- a rollout problem
- or an access-control problem

### 3. Synthetic Checks Must Be Governed

The project now has stronger support assets and test drivers.

The platform console can build on that, but only through pre-approved checks such as:

- health verification
- contract fetch and parse
- known workflow dry-run or synthetic validation

not arbitrary test execution.

## Top-Level Navigation

Suggested top-level modules:

1. Overview
2. Services
3. Compatibility
4. Rollouts
5. Diagnostics
6. Support Access
7. Environments
8. Audit Trail

This differs intentionally from the `Banking Operations Console`, which should stay organized around business entities and workflows.

## Module Design

### 1. Overview

Purpose:

- give operators an immediate cross-environment summary

Key content:

- overall platform status by environment
- current incidents or degraded areas
- rollout summary
- compatibility summary
- recent privileged actions

Typical cards:

- `Prod: Degraded`
- `Staging: Healthy`
- `Deposit Service Canary: 20%`
- `Contract Drift Detected In Audit Service`
- `Support Access Elevation Active`

Primary users:

- all platform personas

### 2. Services

Purpose:

- show service and dependency state by environment

Views:

- service list by environment
- service detail
- dependency detail

Recommended fields:

- liveness
- readiness
- version and build metadata
- deployment revision
- dependency health
- queue or worker state where relevant
- links to logs, traces, dashboards, and runbooks

Design note:

- the console summarizes status
- deeper telemetry remains in the existing observability stack

### 3. Compatibility

Purpose:

- show whether deployed surfaces remain compatible with approved baselines

This module is where recent contract work directly shapes the design.

Compatibility should be shown by surface:

- public service contract
- internal service contract
- Gateway or BFF surface
- platform operator surface

Checks may include:

- OpenAPI document availability
- parse success
- baseline version
- drift summary
- last verified time

Important rule:

- do not mix all routes into one compatibility bucket
- preserve scope labels so operators can see what drift actually matters

### 4. Rollouts

Purpose:

- track deployment progression and risk

Views:

- current rollout stage by service and environment
- canary percentage
- recent version changes
- rollout blockers
- rollback recommendations or links

Example signals:

- target version
- current version
- deployment started at
- last successful health gate
- last compatibility verification

### 5. Diagnostics

Purpose:

- let operators investigate incidents without direct shell access

Views:

- diagnostics overview
- service diagnostics
- dependency diagnostics
- workflow diagnostics
- contract diagnostics
- correlation-based lookup

Search keys:

- environment
- service
- correlation ID
- transaction ID
- transaction number
- account number
- customer number
- run ID

Useful output:

- categorized failures
- recommended next step
- evidence snapshot
- related contract status
- related logs and traces links

Permission model:

- read-heavy
- available to `PlatformOperator` and `SupportEngineer`

### 6. Support Access

Purpose:

- govern platform-side human access rather than burying it in IAM back offices

Views:

- active support sessions
- temporary elevation grants
- expiring privileged access
- environment-scoped access inventory
- break-glass usage

Write actions:

- request support elevation
- approve support elevation
- revoke temporary access

Requirements:

- reason capture
- ticket reference when applicable
- strong audit trail
- time-bound grants

### 7. Environments

Purpose:

- compare environment state in a structured way

Views:

- environment summary
- config diff
- dependency diff
- version diff
- enabled features or rollout flags

Use cases:

- why staging and production differ
- whether canary config diverges from baseline
- whether a support issue is environment-specific

Design note:

- show normalized diffs
- never expose raw secrets

### 8. Audit Trail

Purpose:

- provide the history of privileged platform actions

Views:

- diagnostic runs
- support access changes
- maintenance actions
- compatibility verification runs
- break-glass events

Search filters:

- actor
- environment
- service
- action type
- result
- incident or ticket reference

## Action Tiers

The console should classify actions by risk tier.

### Tier 0. Passive Read

Examples:

- view health summary
- view version metadata
- open logs and traces links
- inspect compatibility results

### Tier 1. Safe Verification

Examples:

- run read-only diagnostics
- fetch and verify runtime contracts
- run approved synthetic checks

Controls:

- authenticated
- audited
- rate-aware

### Tier 2. Controlled Support Actions

Examples:

- grant temporary support access
- mark an incident investigation context
- rerun a bounded diagnostic suite

Controls:

- elevated role
- reason capture
- optional approval

### Tier 3. Privileged Maintenance

Examples:

- retry a failed platform-side repair flow
- requeue controlled operational work
- trigger a higher-cost or side-effecting recovery action

Controls:

- strongest role requirement
- explicit confirmation
- mandatory audit
- optional dual control or step-up authentication

## API Boundary Guidance

The console should not directly depend on every service-specific route shape.

Recommended model:

- basic probe endpoints remain narrow and service-local
- platform summary and diagnostics aggregate under `Gateway` or a dedicated platform surface
- deeper service-level evidence can still come from downstream services

For near-term design, use:

- `/api/v1/system/*` for platform summary and diagnostics
- separate internal routes later if the platform surface grows

Related API draft:

- `docs/37-platform-diagnostics-api-draft.md`

## Data Sources

The console should compose data from distinct source categories.

### Runtime Status Sources

- health endpoints
- readiness endpoints
- service metadata endpoints

### Compatibility Sources

- runtime OpenAPI documents
- approved contract baselines
- contract verification results

### Workflow And Incident Sources

- workflow-state APIs
- queue and backlog summaries
- audit events

### Deep Debug Sources

- logs
- traces
- metrics dashboards
- incident systems

The console should summarize and link, not clone those systems.

## Suggested Delivery Phases

### Phase 1. Read-Only Operator Surface

- overview
- services
- compatibility
- diagnostics summary

### Phase 2. Support And Environment Controls

- support access module
- environment comparison
- stronger audit history

### Phase 3. Governed Verification

- approved synthetic checks
- compatibility verification runs
- richer diagnostics workflows

### Phase 4. Higher-Privilege Maintenance

- carefully scoped repair actions
- stronger approval and escalation controls

## Current Implementation Status

The repository now contains a first implemented version of this console.

Implemented frontend modules:

1. `Overview`
2. `Services`
3. `Compatibility`
4. `Rollouts`
5. `Environments`
6. `Workflows`
7. `Diagnostics`
8. `Maintenance`
9. `Audit`

Implemented backend support today:

- `Gateway` aggregation endpoints under `/api/platform/*`
- service health and dependency summary
- runtime OpenAPI compatibility verification against critical-path baselines
- environment summary for the current runtime
- rollout summary for the current runtime
- deposit workflow detail and correlation-driven diagnostics
- selected maintenance actions for pending review and outbox recovery flows

Current implementation limits:

- compatibility is still a current-state comparison, not historical drift analysis
- rollout data is summarized and local-first, not fed by a real deployment orchestrator
- environment comparison uses a baseline shape but not a true cross-environment diff store yet
- support access controls are designed but not implemented as a first-class module yet
- logs, traces, dashboards, and incident links are still conceptual rather than fully integrated

## Recommended Implementation Roadmap

### Near Term

- persist compatibility snapshots so operators can see drift over time
- add service-level evidence drill-through links for logs, traces, and dashboards
- deepen `Environments` into a true config and version diff module
- make `Rollouts` aware of deployment metadata instead of only health-derived status

### Mid Term

- add a dedicated `Support Access` module with temporary elevation workflows
- add read-only incident context objects and ticket references
- split compatibility into separate surface groups for service, Gateway, BFF, and operator APIs
- formalize governed synthetic checks and run history

### Later

- move higher-risk maintenance actions behind stronger approval and step-up controls
- add multi-environment platform aggregation aligned with Azure-hosted deployments
- separate security administration from platform operations once the IAM model grows

## Current Recommendation

The best next design move is to treat the platform console as the productized operational layer built from recent engineering improvements.

In practice, that means:

- use testing and contract work to define cleaner platform boundaries
- expose only approved and governed verification capabilities
- separate read-only diagnostics from privileged operations
- keep business operations and platform operations on different control planes
