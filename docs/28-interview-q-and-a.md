# Interview Q And A

## Why did you build this project?

I wanted a portfolio project that showed more than CRUD. Banking transactions are a strong example because they force you to think about ownership, consistency, retries, compensation, auditability, and role-specific user experience.

## Why use microservices instead of a monolith?

The goal was to make domain ownership explicit. `Customer`, `Account`, `Deposit`, and `Audit` each own a distinct area. That made it possible to demonstrate cross-service orchestration, internal authentication, and eventual consistency tradeoffs more clearly than a single application would.

## Why is balance ownership in Account Service?

Balances should have a single source of truth. By keeping balance mutations in `Account Service`, the system avoids split ownership and makes reconciliation logic easier to reason about.

Relevant source:

- `src/Banking.Services.Account/Services/AccountService.cs`

## Why not use distributed transactions?

Distributed transactions would couple services more tightly and make infrastructure assumptions stronger. I preferred local consistency inside each service and explicit cross-service orchestration through messaging and status transitions.

## What does the deposit flow demonstrate technically?

It demonstrates:

- `Idempotency-Key` handling
- persisted outbox messages
- RabbitMQ-based async work
- SAGA-style state progression
- compensation
- `PendingReview` for unresolved edge cases

Relevant source:

- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`

## Why separate the operations console and customer portal?

They serve different users and different trust levels. Operators need review and recovery tools. Customers need a simpler and safer interface. Splitting them now makes future authorization and deployment boundaries cleaner.

## How did you test the system?

I used layered testing:

- unit tests for business logic
- integration tests for HTTP and persistence behavior
- contract tests for OpenAPI coverage
- Newman and smoke scripts for end-to-end regression

Relevant source:

- `tests/`
- `scripts/manual-smoke-test.ps1`
- `scripts/run-newman-local.ps1`

## What would you improve next?

- real IAM for customer authentication
- richer read-model/query services
- stronger observability and metrics
- more formal database migration/versioning strategy
- role-based authorization beyond the current local demo model
