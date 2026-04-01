# Platform Identity And Operations Architecture

## Purpose

This document defines the next-stage architecture for:

- user identity
- authentication and authorization
- console separation
- platform operations and support tooling

The current repository already demonstrates business workflows well:

- customer onboarding
- account lifecycle
- deposit orchestration
- audit logging
- customer self-service

The next design step is to treat the platform itself as an operating system for those workflows, not just as a set of business APIs.

## Why This Is Needed

The current system has two visible user experiences:

- `Customer Portal`
- `Operations Console`

That is enough for demonstrating customer and business workflows, but it is not enough for a realistic platform model.

If the product is developed and operated by a vendor or central platform team, a third operational surface is needed:

- `Platform Operations Console`

This console is not for daily bank business operations. It is for platform health, support, diagnosis, maintenance, controlled recovery, and environment management.

Without that separation:

- business users get too much operational power
- platform operators end up using business tools for system maintenance
- security boundaries become blurry
- monitoring and recovery capabilities get mixed into ordinary workflow UI

## Recommended Control Plane Split

The system should evolve into four main user-facing surfaces:

1. `Customer Portal`
2. `Business Operations Console`
3. `Platform Operations Console`
4. `Security / Administration Console`

The first three are the minimum target shape. The fourth can begin as modules inside platform operations and split later if needed.

## Console Responsibilities

### 1. Customer Portal

Primary audience:

- retail or business customers

Primary goals:

- self-service sign-in
- account overview
- transaction history
- customer-safe deposit and withdrawal actions

Must not expose:

- review queues
- audit internals
- internal IDs
- system health
- maintenance actions

### 2. Business Operations Console

Primary audience:

- tellers
- back-office operators
- supervisors
- review and exception-handling staff

Primary goals:

- customer servicing
- account servicing
- deposit workflow tracking
- pending review handling
- business audit visibility

Must not become:

- a system monitoring dashboard
- a deployment tool
- a diagnostic shell
- a maintenance workstation for platform teams

### 3. Platform Operations Console

Primary audience:

- platform operators
- support engineers
- SRE / operations staff
- vendor maintenance staff
- environment administrators

Primary goals:

- service health and dependency status
- queue, worker, and scheduler monitoring
- operational diagnostics
- controlled retry and maintenance actions
- configuration visibility
- support tooling
- test tooling and environment utilities

This console exists because "system control" is a different concern from "business control".

### 4. Security / Administration Console

Primary audience:

- security administrators
- IAM administrators
- compliance and audit administrators

Primary goals:

- identity lifecycle
- role and policy management
- privileged-access workflows
- audit investigation
- break-glass controls

This may start as a module inside the Platform Operations Console, but should be modeled as a distinct responsibility from day one.

## Identity Domains

The platform should stop thinking in terms of just "external" and "internal".

Instead, identity should be modeled as multiple domains:

### Customer Identities

- personal or business customers
- customer self-service only
- narrow data and action scope

### Business User Identities

- teller
- operator
- reviewer
- branch manager
- head-office operations

These identities act on behalf of the bank's business workflows.

### Platform Operator Identities

- operations engineer
- support engineer
- on-call responder
- platform administrator

These identities act on the runtime platform rather than on customer business workflows.

### Vendor / Engineering Identities

- developer
- release engineer
- maintenance engineer

These identities should not automatically have unrestricted production access.

### Service Identities

- service-to-service principals
- workers
- schedulers
- integration accounts

### Test Identities

- synthetic users
- test harness accounts
- smoke-test operators
- non-production support accounts

These identities should be explicit and isolated rather than hidden inside ordinary operator credentials.

## Authentication Strategy

Authentication should be chosen per identity domain, not forced into one mechanism for everything.

### Customers

Recommended direction:

- portal sign-in
- MFA-ready design
- device/session protections

Current lightweight session-based sign-in is acceptable for the existing project stage, but it should be treated as a customer-specific auth path, not a universal pattern.

### Business Users

Recommended direction:

- enterprise identity provider
- SSO
- MFA
- short-lived sessions

### Platform Operators

Recommended direction:

- stronger MFA
- network restrictions or jump-host access where appropriate
- privileged session controls
- higher audit requirements

### Service Identities

Recommended direction:

- signed tokens, mTLS, or centrally managed service credentials
- rotation support
- no shared human credentials

The current API-key model is a practical intermediate step, but it should be documented as transitional.

### Test And Support Accounts

Recommended direction:

- environment-scoped identities
- explicit naming and ownership
- automatic expiry when possible
- audit trail for creation and use

## Authorization Model

Authorization should evolve into a layered model.

### Layer 1: Principal Type

Distinguish at least:

- customer
- business-user
- platform-operator
- vendor-engineer
- service
- test-account

This is broader than the current:

- `external-client`
- `internal-service`

### Layer 2: Role-Based Access Control

Examples:

- `Customer`
- `Teller`
- `BackOfficeOperator`
- `DepositReviewer`
- `BranchManager`
- `PlatformOperator`
- `SupportEngineer`
- `SecurityAdmin`
- `ReleaseManager`

### Layer 3: Scope-Based Access Control

Examples:

- branch
- region
- tenant
- environment
- customer ownership
- account ownership
- platform area

### Layer 4: Privileged Action Controls

For sensitive actions:

- step-up authentication
- dual approval
- time-bound elevation
- break-glass procedures
- explicit reason capture

## Separation Of Duties

The design should explicitly prevent accidental power concentration.

Examples:

- a business operator should not deploy services
- a platform operator should not approve business transactions
- a vendor engineer should not automatically read broad customer data
- a support engineer should not silently modify balances
- a test account should never inherit production platform privileges

This separation matters more as soon as the system gains maintenance tools and controlled repair paths.

## Platform Operations Console Capabilities

The platform console should include at least the following modules.

### 1. Runtime Overview

- service availability
- dependency availability
- worker health
- queue depth
- scheduler status
- recent incidents and alerts

### 2. Workflow State Monitoring

- deposit saga stage breakdown
- pending review aging
- outbox backlog
- compensation failure counts
- audit write failure counts
- idempotency replay storm indicators

### 3. Diagnostics

- correlation ID search
- request and error tracing
- structured log lookup
- latency and throughput charts
- downstream dependency error inspection

### 4. Controlled Maintenance Actions

- retry failed workflow step
- republish outbox item
- re-run compensation
- mark incident state
- disable or drain background worker
- view configuration snapshot

These actions should be controlled, audited, and role-bound.

### 5. Environment And Test Utilities

- smoke test runner
- synthetic transaction runner
- test account inventory
- environment data reset tools for non-production
- support-only diagnostics

### 6. Account And Access Governance

- platform account inventory
- privileged access review
- service identity inventory
- credential rotation status
- test account ownership and expiry

## Monitoring Model

The platform should move beyond simple health checks.

### Current Minimum

- `/api/v1/health`
- `/api/v1/ready`

### Needed Next

- dependency-specific readiness
- queue and worker status endpoints
- workflow counters
- retry backlog metrics
- stale review queue detection
- audit pipeline status
- platform alerts and threshold summaries

Health is not only "is the process up?".

The platform needs to answer:

- Is the workflow making progress?
- Are messages stuck?
- Are retries increasing?
- Is a dependency degraded?
- Is a worker alive but ineffective?

## Maintenance And Support Tooling

The platform design should explicitly include support and maintenance tooling as first-class features.

Examples:

- operator-safe retry actions
- support engineer runbooks
- read-only diagnosis pages
- non-production repair scripts
- data consistency checkers
- synthetic monitoring hooks

These tools should not live only as private scripts. Important operational actions need discoverable, permissioned surfaces.

## Test Account Design

Test account design deserves its own model.

Recommended principles:

- explicit account type or metadata
- owned by a team or process
- environment-scoped
- time-bounded where possible
- isolated from production business roles
- excluded from normal customer reporting when appropriate

Suggested categories:

- UI demo customers
- integration-test customers
- smoke-test operator accounts
- synthetic monitoring accounts
- support sandbox accounts

## Recommended Architecture Evolution

### Phase 1: Document And Partition

- define identity domains
- define console boundaries
- define platform-operations scope
- document transitional security model

### Phase 2: Expand Shared Security Primitives

- extend principal types
- add richer policies
- separate business and platform authorization rules
- add audit events for privileged actions

### Phase 3: Add Platform Operations APIs

- aggregated health
- workflow monitoring endpoints
- queue and worker diagnostics
- controlled retry and maintenance endpoints

### Phase 4: Build Platform Operations Console

- overview dashboard
- diagnostics views
- workflow repair tools
- account and environment governance modules

### Phase 5: Mature IAM

- external identity provider integration
- stronger platform MFA
- privilege elevation workflows
- service identity hardening

## Near-Term Code Implications For This Repository

The current codebase suggests these near-term changes:

### Security Model

Expand shared identity primitives in `Banking.BuildingBlocks.Security` beyond:

- `ExternalClientOnly`
- `InternalServiceOnly`

Toward policies such as:

- `CustomerOnly`
- `BusinessUserOnly`
- `PlatformOperatorOnly`
- `SecurityAdministratorOnly`
- `PrivilegedMaintenanceAction`

### Edge Services

Clarify entry-layer roles:

- `Banking.Bff.CustomerPortal` for customer-scoped access
- `Banking.Gateway` for operator and platform entry concerns

### Platform Monitoring

Add a platform-focused API surface before adding a full UI:

- aggregated health
- dependency status
- deposit workflow metrics
- retry queue visibility
- audit pipeline status

### Auditing

Extend audit design to capture:

- privileged console actions
- maintenance operations
- role changes
- support interventions

## Trade-Offs

### Why Not Keep One Console

Because one console would mix:

- customer servicing
- business operations
- platform maintenance
- privileged engineering actions

That makes both security and usability worse.

### Why Not Build Full IAM First

Because the platform first needs the correct boundaries and domain model.

Defining:

- who the identities are
- what they can do
- where they should operate

is more valuable right now than jumping directly into a heavy implementation.

### Why Not Leave Support Tools As Scripts

Because scripts are useful, but they are not enough for a controlled platform:

- they are harder to govern
- harder to audit
- harder to delegate safely
- harder to make discoverable for support teams

## Outcome

After this architecture is adopted, the system becomes easier to reason about:

- customer interactions stay customer-safe
- business operations stay focused on banking workflows
- platform operators get their own control plane
- security and support concerns stop leaking into ordinary business tooling

This is the right foundation for the next stage of the project.
