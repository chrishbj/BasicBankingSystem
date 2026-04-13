# Platform Identity And Operations Architecture

## Purpose

This document defines the next-stage architecture for:

- platform-side identity and authorization
- separation between business operations and platform operations
- the future `Platform Management Console`
- the relationship between diagnostics, contracts, testing assets, and runtime support tooling

It builds on the recent testing and contract-boundary work in:

- `docs/33-test-design-standards.md`
- `docs/34-testing-roadmap.md`
- `docs/35-test-design-findings-and-remedies.md`
- `docs/36-platform-diagnostics-and-advanced-health-checks.md`
- `docs/37-platform-diagnostics-api-draft.md`

## Why This Is Needed

The current repository already has:

- a `Customer Portal`
- a business-oriented `Operations Console`
- health endpoints
- OpenAPI contract tests
- integration-test infrastructure
- support code for synthetic workflows and verification

Those capabilities are useful individually, but they currently live in different categories:

- product features
- test assets
- engineering support code
- design drafts

The next platform stage should connect them into a clearer control-plane model.

The design goal is not to expose developer tools directly to operators.

The design goal is to convert selected engineering assets into governed platform capabilities:

- health and readiness become probe surfaces
- contract verification becomes operator diagnostics
- synthetic workflow support becomes controlled platform verification
- privileged maintenance moves behind stronger role and audit boundaries

## Core Architectural Insight

The system should not treat all "operations" as one concern.

It should separate at least three control planes:

1. customer self-service
2. business operations
3. platform operations

The current `Operations Console` is strong at business workflows:

- customer servicing
- account servicing
- transaction review
- approvals
- audit and business history
- institution administration
- role-based business permissions

But platform operations are materially different. They focus on:

- service health by environment
- deployed build and version visibility
- API and contract compatibility
- rollout and canary status
- incident diagnostics
- support access controls
- environment and config comparison
- logs and traces entry points

Those concerns should not be mixed into the business console indefinitely.

## Recommended Surface Split

The system should evolve into these user-facing surfaces:

1. `Customer Portal`
2. `Banking Operations Console`
3. `Platform Management Console`
4. `Security And Access Administration`

`Security And Access Administration` may begin as modules within the platform console, but it should be treated as a distinct responsibility from day one.

## Design Principles

### 1. Reuse Engineering Assets Without Leaking Engineering Privilege

The project already contains assets that are useful for platform operations:

- health checks
- readiness checks
- OpenAPI and contract verification
- shared integration-test infrastructure
- synthetic test drivers
- workflow diagnostics ideas

These should be reused selectively, but they must be repackaged as:

- authenticated
- auditable
- rate-aware
- environment-aware
- production-safe

platform capabilities.

### 2. Keep Contract Scope Explicit

Recent contract-test work clarified that:

- public service APIs
- internal service APIs
- Gateway routes
- BFF routes
- demo endpoints
- platform-only endpoints

should not silently collapse into one contract surface.

That same lesson applies to platform design:

- the platform console can observe many surfaces
- it should not redefine them as one undifferentiated API domain

The console should display boundary-aware information rather than hide boundary mistakes.

### 3. Separate Read-Only Diagnostics From Control Actions

This distinction is foundational.

Read-only platform actions include:

- health summary
- contract visibility
- runtime diagnostics
- config comparison
- rollout visibility

Privileged control actions include:

- rerunning advanced checks
- retrying failed maintenance workflows
- requeueing operational work
- enabling temporary support access

These categories must not share the same permissions or audit expectations.

### 4. Treat Synthetic Verification As A Productized Platform Capability

Some current test and smoke assets are good seeds for platform verification, but only after they are constrained.

The platform console should support:

- approved synthetic checks
- non-destructive verification runs
- pre-defined environment diagnostics

It should not expose arbitrary developer test execution in production.

### 5. Preserve Source-Of-Truth Ownership

The platform console is a control plane, not the source of truth for every domain.

Ownership remains with:

- services for runtime state
- OpenAPI docs and contract tests for interface baselines
- audit systems for privileged action history
- observability tools for deep raw telemetry

The platform console should aggregate high-value signals and provide drill-through into those sources.

## Identity Domains

The current system should evolve from broad categories like "external" and "internal" toward more explicit identity domains.

### Customer Identities

- self-service customers
- narrow data and action scope

### Business User Identities

- tellers
- operators
- reviewers
- supervisors
- institution admins

### Platform Operator Identities

- support engineers
- on-call responders
- SRE operators
- platform administrators

### Security And Access Identities

- IAM administrators
- security administrators
- compliance or audit administrators

### Service Identities

- service-to-service principals
- workers
- schedulers
- integration accounts

### Synthetic And Test Identities

- smoke-test operators
- synthetic monitoring identities
- non-production support accounts
- temporary validation principals

These must be explicit identities, not hidden reuse of ordinary business credentials.

## Authorization Model

Authorization should evolve in layers.

### Layer 1. Principal Type

At minimum:

- customer
- business-user
- platform-operator
- security-admin
- service
- synthetic-check

### Layer 2. Role

Examples:

- `Teller`
- `DepositReviewer`
- `InstitutionAdmin`
- `PlatformOperator`
- `SupportEngineer`
- `SREOperator`
- `PlatformAdmin`
- `SecurityAdmin`

### Layer 3. Scope

Examples:

- tenant or institution
- branch or business region
- environment
- platform module
- support case

### Layer 4. Privileged Action Controls

Examples:

- reason capture
- incident or ticket reference
- time-bounded elevation
- approval requirements
- step-up authentication

## Separation Of Duties

The platform design should explicitly prevent power concentration.

Examples:

- business operators should not run platform maintenance
- platform operators should not perform broad customer business actions by default
- support engineers should not automatically gain deploy rights
- synthetic identities should never inherit full human admin permissions
- contract-verification tools should not be able to perform arbitrary write actions

## Architectural Relationship To Testing And Contracts

The recent testing work changes how the platform should be designed.

### From `docs/33-test-design-standards.md`

The platform should preserve clear test-layer and runtime-layer boundaries.

That means:

- platform diagnostics may reuse test patterns
- platform diagnostics should not pretend to be unit or integration tests
- each platform capability should state whether it is probe, operator diagnostic, or privileged control

### From `docs/34-testing-roadmap.md`

The platform should anticipate separate contract surfaces:

- public service contracts
- internal service contracts
- app-facing Gateway or BFF surfaces
- operator-only platform surfaces

That means the future platform console should expose compatibility views by surface, not by one mixed contract list.

### From `docs/35-test-design-findings-and-remedies.md`

The platform should avoid boundary blur.

That applies operationally as well:

- diagnostic failures should point to one layer or one dependency
- platform checks should make failure ownership clearer
- shared infrastructure should be reused without hiding intent

## Recommended Evolution Path

### Phase 1. Documentation And Boundary Definition

- finalize the control-plane split
- define platform roles and permission tiers
- align platform docs with contract and testing boundaries

### Phase 2. Read-Only Platform Surface

- aggregate service health
- expose version and environment metadata
- add runtime contract visibility
- add diagnostics overview and drill-through links

### Phase 3. Governed Verification And Support Tooling

- introduce authenticated diagnostic runs
- productize selected synthetic checks
- add support access governance and audit trails

### Phase 4. Controlled Maintenance Actions

- add tightly permissioned repair workflows
- require explicit reason capture and stronger audit
- add approval and step-up paths for higher-risk actions

## Current Implementation Snapshot

The repository has now moved beyond a purely conceptual platform design.

Current implemented assets include:

- a dedicated `Banking.PlatformOps` React application
- a Gateway-hosted platform API surface under `/api/platform/*`
- service health aggregation
- deposit workflow summary and runtime worker visibility
- correlation-based diagnostics for deposit and audit activity
- read-only compatibility checks that fetch runtime `openapi/v1.json` documents
- rollout summary cards for the current environment
- environment snapshot summary for local and Docker-based validation
- controlled maintenance actions for selected deposit review and outbox flows

This means the project already demonstrates the beginning of a real platform control plane, even though the identity and IAM model is still simplified for the demo environment.

The implementation also validates a key architectural assumption from this document:

- testing and contract assets can be productized into operator-facing capabilities
- but they should be exposed through explicit platform APIs and roles instead of direct developer-tool access

## Remaining Gaps

The current implementation is intentionally early-stage.

Important gaps still remain:

- no real external identity provider or Azure-native IAM integration yet
- no separate `Security And Access Administration` module yet
- no time-bound support access approval workflow yet
- no historical compatibility baseline store yet
- no true multi-environment comparison with staged snapshots
- no production-grade drill-through integration with logs, traces, dashboards, or incident systems

## Recommended Next Push

The strongest next architecture steps are:

1. make platform identity more explicit by separating `platform-operator`, `support-engineer`, and future `security-admin` capabilities
2. add a governed support-access model with reason capture, duration, and audit
3. persist compatibility verification history so drift can be shown over time rather than only as a current snapshot
4. add true multi-environment summaries for `local`, `docker`, `staging`, and `production-like` baselines
5. keep platform APIs boundary-aware by continuing to distinguish public service contracts, operator surfaces, and app-facing gateway surfaces

## Current Recommendation

The next practical step is not to build a huge new console immediately.

The next practical step is to define `Platform Management Console` as a control plane built from:

- explicit contract boundaries
- layered diagnostics
- governed synthetic verification
- stronger platform identity and role separation

Related documents:

- `docs/39-platform-operations-console-detailed-design.md`
- `docs/36-platform-diagnostics-and-advanced-health-checks.md`
- `docs/37-platform-diagnostics-api-draft.md`
