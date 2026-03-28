# Basic Banking System Requirements Draft

## Goal

Build a scalable basic banking system based on the `ReactFullStackDemo` engineering style. Phase 1 focuses on:

- Customer management
- Account opening
- Deposit processing
- Audit trail
- Transaction consistency foundations

## Principles

- Frontend/backend separation
- Domain-oriented microservices
- Strong auditability for money-related operations
- Event-driven consistency with SAGA
- Idempotency for critical write APIs
- Horizontal scalability from day one

## Phase 1 Scope

### In Scope

- Customer onboarding and customer status management
- Current/checking account opening
- Deposit intake and posting
- Transaction lookup
- Audit log recording
- Basic authentication and authorization
- Reliability foundations for consistency and compensation

### Out of Scope

- Transfers
- Withdrawals
- Loans
- Credit cards
- Interest settlement
- Risk scoring
- Complex reporting
- External clearing integrations

## Core Business Objects

- `Customer`
- `Account`
- `DepositTransaction`
- `AuditLog`

## Key Functional Requirements

### Customer Management

- Create customer
- Get customer details
- Search customers with pagination
- Update contact information
- Change customer status
- Enforce uniqueness for identity number, mobile, and customer number

### Account Management

- Open account for an active customer
- Support multiple accounts per customer
- Track account status and currency
- Allow closing only when balance is zero

### Deposit System

- Accept deposits from counter or digital channels
- Require idempotency support
- Validate customer and account status
- Update balance and transaction history on success
- Return explicit error reasons on failure

### Audit

- Record audit events for customer creation, update, account opening, and deposits
- Store actor, target, timestamps, before/after snapshots, and correlation identifiers

## Non-Functional Requirements

- Stateless APIs
- Independent scaling by service
- Structured logging and distributed tracing
- Retry, timeout, circuit breaker, and rate limiting
- Outbox pattern for event publishing
- Consumer idempotency
- Role-based authorization

## Acceptance Criteria for Phase 1

- Customer can be created and queried
- Active customer can open an account
- A deposit updates the account balance
- Repeated submission with the same idempotency key does not double-post
- Transaction history can be queried
- Audit logs can be queried

## Next Iterations

- Phase 2: withdrawal, freeze/unfreeze, notifications
- Phase 3: transfers and richer SAGA orchestration
- Phase 4: reporting, risk, limits, approvals, external integrations
