# Phase 1 Testing Strategy and TDD

## Goals

Protect the highest-risk banking behaviors:

- core customer, account, and deposit rules
- idempotency
- concurrency-safe balance updates
- audit completeness
- API contract stability
- gateway and BFF boundary behavior

## Testing Pyramid

- 70% unit tests
- 20% integration tests
- 10% contract and end-to-end tests

## Recommended Tools

- `xUnit`
- `FluentAssertions`
- `Moq`
- `WebApplicationFactory`
- SQLite-backed test hosts for service API integration tests
- one maintained OpenAPI document for documented public service APIs
- Postman and Newman for regression execution

Optional later-stage additions:

- `Playwright` for browser-level end-to-end flows
- containerized dependencies only when SQLite-backed hosts are no longer sufficient for the scenario

## TDD Workflow

`Red -> Green -> Refactor`

### Red

- start with a failing test for the smallest business rule

### Green

- implement the minimum behavior needed to pass

### Refactor

- remove duplication
- extract clear builders, drivers, and shared helpers

## First Test Wave

### Customer

- create customer succeeds for valid request
- duplicate identity fails
- duplicate mobile fails
- invalid status transition fails
- pagination and error payloads stay stable

### Account

- open account succeeds for active customer
- open account fails for frozen customer
- close account fails when balance is non-zero
- insufficient funds returns the expected conflict contract
- activities filtering and pagination stay stable

### Deposit

- deposit amount must be positive
- closed account rejects deposit
- repeated idempotency key does not double-post
- concurrent deposits preserve final balance
- review and not-found API contracts stay stable

### Audit And Platform

- audit event contains before and after snapshots and correlation id
- health returns `200`
- unauthorized requests return `401`
- paged audit queries return stable metadata

### Gateway And BFF

- missing API key returns `401`
- downstream status codes are preserved by the gateway
- invalid portal login returns `401 ProblemDetails`
- cross-customer account access is rejected

### Contract

- required public service paths and methods stay present in `openapi-phase1.yaml`
- stale endpoints are removed from the contract document
- critical shared schemas such as `ProblemDetails` and paged responses stay stable
- contract tests use structural parsing rather than raw text matching

## CI Recommendation

1. unit tests
2. contract tests
3. integration tests
4. end-to-end tests

## Current Repository Direction

The current repository baseline is:

- service unit tests isolate dependencies with `Moq`
- service integration tests use `WebApplicationFactory` plus a shared SQLite host base
- integration tests assert both success contracts and failure contracts such as `ProblemDetails`, pagination, filtering, and sorting
- contract tests treat `docs/openapi-phase1.yaml` as the current source for documented backend service APIs
- request builders and async polling helpers live in local `Support/` folders instead of being duplicated across tests
