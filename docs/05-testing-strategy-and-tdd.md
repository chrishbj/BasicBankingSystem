# Phase 1 Testing Strategy and TDD

## Goals

Protect the highest-risk banking behaviors:

- Core customer, account, and deposit rules
- Idempotency
- Concurrency-safe balance updates
- Audit completeness
- API contract stability

## Testing Pyramid

- 70% unit tests
- 20% integration tests
- 10% contract and end-to-end tests

## Recommended Tools

- `xUnit`
- `FluentAssertions`
- `WebApplicationFactory`
- `Testcontainers`
- `Playwright`

## TDD Workflow

`Red -> Green -> Refactor`

### Red

- Start with a failing test for the smallest business rule

### Green

- Implement the minimum behavior needed to pass

### Refactor

- Remove duplication
- Extract value objects and common abstractions

## First Test Wave

### Customer

- Create customer succeeds for valid request
- Duplicate identity fails
- Duplicate mobile fails
- Invalid status transition fails

### Account

- Open account succeeds for active customer
- Open account fails for frozen customer
- Close account fails when balance is non-zero

### Deposit

- Deposit amount must be positive
- Closed account rejects deposit
- Repeated idempotency key does not double-post
- Concurrent deposits preserve final balance

### Audit and Platform

- Audit event contains before/after snapshots and correlation id
- Health returns `200`
- Unauthorized requests return `401`

## CI Recommendation

1. Unit tests
2. Contract tests
3. Integration tests
4. End-to-end tests
