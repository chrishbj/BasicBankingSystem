# Interview Q And A

## Why did you build this project?

I wanted a portfolio project that showed real engineering tradeoffs instead of just CRUD screens and simple APIs. Banking is a good domain for that because even a basic deposit flow forces you to think about ownership, idempotency, auditability, retries, compensation, and operator tooling.

## Why use multiple backend services instead of one API?

The main reason was to make domain ownership explicit. Customer, account, deposit, and audit all have different responsibilities. Splitting them made it easier to demonstrate service boundaries, cross-service orchestration, and eventual consistency tradeoffs in a way that a single application would hide.

## Why is balance ownership in Account Service?

Balance needs one source of truth. By keeping balance mutations inside `Account Service`, the system avoids split ownership and makes reconciliation easier to reason about. `Deposit Service` owns workflow state, but it does not own the ledger balance itself.

Relevant source:

- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`

## Why did you create both Gateway and BFF?

They serve different entry-layer concerns. The gateway is the unified entry point for the operations console and the platform control plane. The customer portal BFF is a separate session-aware backend that aggregates customer-facing data and enforces customer ownership checks.

Relevant source:

- `src/Banking.Gateway/`
- `src/Banking.Bff.CustomerPortal/`

## Why are there three frontend experiences?

Because the repository intentionally separates:

- operator workflows
- customer self-service workflows
- platform monitoring and maintenance workflows

That keeps the UX, trust boundary, and permission model clearer than trying to force everything into one frontend.

## Why not use distributed transactions?

Distributed transactions would make service coupling stronger and increase infrastructure assumptions. I preferred local consistency inside each service and explicit cross-service coordination through outbox, asynchronous processing, status transitions, compensation, and pending review.

## What does the deposit flow demonstrate technically?

It demonstrates:

- `Idempotency-Key` handling
- persisted outbox messages
- asynchronous processing through RabbitMQ or in-memory transport
- saga-style workflow progression
- compensation
- automatic retry and manual review through `PendingReview`

Relevant source:

- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

## What security model does the project use?

It uses two models. Backend microservices use header-based authentication with API keys for external callers and service identity headers for internal calls. The customer portal BFF uses session-based authentication and then applies customer ownership checks before returning protected data.

Relevant source:

- `src/Banking.BuildingBlocks/Security/`
- `src/Banking.Bff.CustomerPortal/Auth/`

## How did you handle internal service-to-service authentication?

I used a shared `DelegatingHandler` that automatically adds internal service identity headers to outgoing `HttpClient` calls. That keeps service identity injection consistent and avoids duplicating header logic in business code.

Relevant source:

- `src/Banking.BuildingBlocks/Security/InternalServiceAuthenticationDelegatingHandler.cs`

## What role does the platform console play?

It gives the project a platform-facing control plane, not just a business UI. It lets me expose service summaries, workflow backlog, correlation diagnostics, maintenance actions, and platform audit views through the gateway.

Relevant source:

- `src/Banking.PlatformOps/`
- `src/Banking.Gateway/Controllers/PlatformController.cs`

## How did you test the system?

I used layered testing:

- unit tests for business logic and side effects
- integration tests for HTTP, persistence, proxy, and BFF behavior
- contract tests for OpenAPI structure
- Newman and smoke scripts for broader regression coverage

Relevant source:

- `tests/`
- `docs/22-testing-and-quality.md`
- `docs/33-test-design-standards.md`

## Which design patterns are most important in this project?

The most important ones are:

- `API Gateway`
- `BFF`
- `Outbox`
- `Saga`
- `Repository`
- `Idempotency`
- `Background Worker`
- `Policy-Based Authorization`

## What would you improve next?

- stronger customer IAM instead of the current demo-friendly sign-in flow
- tighter authorization on some deposit maintenance endpoints
- richer production-style observability and alerting
- more formal migration/versioning discipline for schema evolution
- more reusable resource-based authorization components in the BFF and services
