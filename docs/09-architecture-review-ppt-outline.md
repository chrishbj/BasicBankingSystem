# Architecture Review PPT Outline

## Slide 1. Title

- Basic Banking System
- Phase 1 Architecture Review
- Scope: Customer, Account Opening, Deposit, Audit

## Slide 2. Why This Project

- Banking systems need more than CRUD
- We must design for correctness, auditability, and scale
- Phase 1 establishes the core transaction backbone

## Slide 3. Phase 1 Scope

- Customer onboarding and status
- Account opening
- Deposit processing
- Audit logging
- Basic auth, tracing, and health checks

## Slide 4. Out of Scope

- Transfers
- Withdrawals
- Loans
- Interest
- Risk engine
- External clearing

## Slide 5. Why Not a Single API

- Ledger ownership becomes unclear
- Concurrency risk increases
- Audit becomes tightly coupled
- Later transfer features become expensive to retrofit

## Slide 6. Target Architecture

- Show overall architecture diagram
- Explain gateway, services, databases, bus, and observability

## Slide 7. Bounded Contexts

- Customer Service
- Account Service
- Deposit Service
- Audit Service
- Query Service

## Slide 8. Deposit End-to-End Flow

- Request accepted by Deposit Service
- Event-driven balance posting by Account Service
- Audit and read-model update downstream

## Slide 9. Why SAGA

- Cross-service workflow
- Avoid distributed 2PC
- Better scalability and cloud fit
- Clear compensation model

## Slide 10. State Models

- Customer states
- Account states
- Deposit transaction states

## Slide 11. Audit and Compensation

- Audit is independent
- Audit failure does not roll back posted funds
- Retry, alerting, and compensation are mandatory

## Slide 12. Testing and TDD

- OpenAPI-first
- Unit, integration, contract, and E2E tests
- Start with `Create Customer`

## Slide 13. Risks and Mitigations

- Duplicate posting
- Balance corruption under concurrency
- Missing audit records
- Unclear service ownership

## Slide 14. Delivery Plan

- Freeze contracts
- Build skeleton
- Implement `Create Customer`
- Implement `Open Account`
- Implement `Create Deposit`

## Slide 15. Decision Needed

- Approve phase 1 scope
- Approve service boundaries
- Approve consistency strategy
- Approve TDD-first delivery approach
