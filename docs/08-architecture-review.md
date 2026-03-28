# Phase 1 Architecture Review

## Review Objective

Confirm that Phase 1 is narrow enough to deliver, but strong enough to become the foundation for later banking capabilities.

Phase 1 focuses on:

- Customer management
- Account opening
- Deposit processing
- Audit trail

## Why Not Keep a Single API

The original demo pattern works well for generic CRUD, but banking requires:

- Balance correctness
- Transaction traceability
- Idempotency
- Auditability
- Scalability under concurrency

That is why the backend is split by domain from the beginning.

## Proposed Service Boundaries

- `Customer Service`: customer master data and status
- `Account Service`: account lifecycle and balance ownership
- `Deposit Service`: deposit intake, idempotency, transaction state
- `Audit Service`: audit persistence and compensation
- `Query Service`: read models and aggregated views

## Why This Split

- Balance ownership stays in one place
- Deposit orchestration stays separate from ledger ownership
- Audit remains independent and does not block core ledger success
- Later transfer workflows can reuse the same orchestration pattern

## Consistency Strategy

- Local transactions within each service
- Event-driven communication across services
- SAGA for long-running cross-service flows
- Outbox pattern for reliable event publication

## Deposit Workflow Summary

1. Deposit Service accepts the request
2. Idempotency is validated
3. A transaction is created with `Received`
4. `DepositRequested` is published
5. Account Service validates and posts balance
6. `DepositPosted` or `DepositRejected` is published
7. Deposit Service updates final status
8. Audit and Query services consume downstream events

## Audit and Compensation

- Audit is separated from business logs
- Audit failure does not roll back a successful posted deposit
- Audit failure must trigger retry, alerting, and compensation

## Main Risks and Mitigations

### Duplicate Deposit

- `Idempotency-Key`
- uniqueness rules
- dedicated tests

### Incorrect Balance Under Concurrency

- Account Service owns balance
- transactional updates
- concurrency tests

### Audit Gaps

- independent audit service
- retry queue
- dead-letter and alerting

## Delivery Recommendation

If approved, implementation should proceed in this order:

1. Freeze API contracts
2. Create solution and test skeleton
3. Start TDD with `Create Customer`
4. Add `Open Account`
5. Add `Create Deposit`
6. Finish audit, monitoring, and end-to-end coverage
