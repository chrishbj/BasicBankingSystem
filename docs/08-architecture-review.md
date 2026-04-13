# Current Architecture Review

## Review Objective

Confirm that the current implementation has clear service boundaries, a credible reliability model for deposit processing, and a practical split between operator-facing and customer-facing entry points.

## What Is Implemented Today

The current repository implements:

- `Banking.Gateway` for the operations console and platform operations
- `Banking.Bff.CustomerPortal` for customer-facing session-based flows
- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

This is no longer a proposal that includes a `Query Service`. The implemented read experience is currently handled by:

- direct service queries through `Gateway`
- BFF-side aggregation for the customer portal
- platform monitoring aggregation inside `Gateway`

## Why The Backend Is Split By Domain

The codebase separates business ownership by domain:

- `Customer Service` owns customer master data and lifecycle state
- `Account Service` owns account lifecycle and balance state
- `Deposit Service` owns deposit intake, idempotency, workflow state, outbox, retry, and review logic
- `Audit Service` owns audit persistence

This split is useful because it keeps:

- balance ownership in one place
- transaction orchestration in one place
- audit storage independent from core account posting

## Entry Layer Design

### Operations Console Path

The operations frontend, `Banking.Web`, does not call each backend service directly. It calls path prefixes such as `/customer-api`, `/account-api`, `/deposit-api`, and `/audit-api`, and `Banking.Gateway` proxies those requests to the correct downstream service.

That gives the system:

- a single backend entry point for operators
- centralized auth, routing, and monitoring endpoints
- a place to expose platform diagnostics and maintenance APIs

Relevant source:

- `src/Banking.Gateway/Program.cs`
- `src/Banking.Gateway/Controllers/ProxyController.cs`
- `src/Banking.Gateway/Services/GatewayProxyService.cs`

### Customer Portal Path

The customer portal uses a separate entry pattern. `Banking.CustomerPortal` talks only to `Banking.Bff.CustomerPortal`, and the BFF then calls customer, account, and deposit services.

That gives the system:

- browser-friendly session authentication
- customer-scoped resource checks
- frontend-oriented aggregation instead of raw microservice payloads

Relevant source:

- `src/Banking.Bff.CustomerPortal/Program.cs`
- `src/Banking.Bff.CustomerPortal/Controllers/`

## Security Model

There are two security models in the codebase.

### Shared Backend Security

Backend services use a shared header-based authentication model:

- external callers use `X-Api-Key`
- internal service-to-service traffic uses `X-Service-Name` and `X-Service-Key`

Authorization is policy-based and centralized in the shared building blocks layer.

Relevant source:

- `src/Banking.BuildingBlocks/Extensions/BankingServiceCollectionExtensions.cs`
- `src/Banking.BuildingBlocks/Security/BankingHeaderAuthenticationHandler.cs`
- `src/Banking.BuildingBlocks/Security/BankingSecurityHeaderValidator.cs`

### Customer Portal Security

The BFF uses session-based authentication and then performs customer ownership checks before returning accounts, activities, or deposit data.

Relevant source:

- `src/Banking.Bff.CustomerPortal/Auth/`
- `src/Banking.Bff.CustomerPortal/Controllers/AccountsController.cs`

## Consistency Strategy

The system does not use distributed transactions across services.

Instead, the design is:

- local database consistency inside each service
- asynchronous workflow progression for deposit processing
- outbox for reliable message publication
- saga-style compensation for partial failure
- pending review plus retry for operational recovery

This is the strongest architectural part of the codebase today.

## Deposit Workflow Summary

The deposit path works like this:

1. `Deposit Service` validates the request
2. it checks `Idempotency-Key`
3. it stores `DepositTransaction` and `DepositOutboxMessage` together
4. it returns `202 Accepted`
5. a hosted outbox dispatcher publishes the message
6. a consumer triggers `DepositTransactionProcessor`
7. the processor calls `Account Service` to post the balance
8. the processor writes audit via `Audit Service`
9. if post-processing fails after account posting, compensation is attempted
10. if compensation fails, the transaction moves to `PendingReview`
11. an automatic retry worker or a manual platform action can continue recovery

Relevant source:

- `src/Banking.Services.Deposit/Program.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

## Why This Architecture Is Credible

The current implementation demonstrates more than CRUD because it includes:

- a dedicated gateway and a dedicated BFF
- explicit service boundaries
- policy-based security
- asynchronous orchestration
- idempotency
- reliable outbox dispatch
- compensation and pending review
- platform monitoring and maintenance APIs

## Current Risks And Improvement Areas

The codebase is strong as a demo and architecture portfolio, but there are still clear next-step improvements:

- some deposit maintenance endpoints are still broader than ideal in authorization scope
- customer portal sign-in is intentionally simple and should evolve toward stronger IAM
- platform-level observability is useful, but production-grade metrics and alerting could be richer
- schema evolution currently relies on startup compatibility work rather than a more formal migration discipline

## Review Recommendation

For the current implementation, the architecture review should emphasize:

1. why the gateway and BFF are separate
2. why balance ownership stays in `Account Service`
3. why deposit orchestration stays in `Deposit Service`
4. how outbox, retry, and pending review provide reliability
5. how platform operations are exposed through `Gateway`
