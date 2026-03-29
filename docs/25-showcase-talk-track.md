# Showcase Talk Track

## Purpose

This is a short presentation script for introducing the project in an interview, portfolio review, or technical discussion.

## 30-Second Version

`BasicBankingSystem` is a banking platform prototype I built to demonstrate microservice boundaries, asynchronous transaction handling, SAGA-style compensation, idempotent APIs, and full-stack delivery with React frontends, automated tests, and OpenAPI documentation.

## 2-3 Minute Version

### 1. Problem Framing

I wanted a project that was more realistic than CRUD, but still small enough to understand end-to-end. Banking transactions are a good fit because they require:

- clear domain ownership
- transaction safety
- recovery behavior
- strong API discipline

### 2. Architecture

I split the backend into:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

The most important design choice is that balances are owned by `Account Service`, while `Deposit Service` coordinates the workflow.

Relevant source:

- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`

### 3. Transaction Handling

The deposit flow is intentionally not a simple synchronous write. It demonstrates:

- `Idempotency-Key` handling
- persisted outbox records
- RabbitMQ-based async processing
- SAGA-style compensation
- `PendingReview` fallback when automation cannot finish safely

Relevant source:

- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

### 4. Frontend Surfaces

I built two different React frontends:

- an operator console for staff workflows
- a customer portal for customer-safe flows

That separation demonstrates role-appropriate UX and clearer future security boundaries.

Relevant source:

- `src/Banking.Web/`
- `src/Banking.CustomerPortal/`

### 5. Quality And Documentation

I also treated testing and documentation as part of the engineering work:

- unit tests
- integration tests
- OpenAPI contract checks
- Swagger support
- Postman and Newman regression assets

Relevant source:

- `tests/`
- `docs/openapi-phase1.yaml`

## Likely Interview Questions

### Why use microservices here?

To demonstrate clear domain ownership and cross-service workflow design. It also makes tradeoffs like eventual consistency and compensation explicit.

### Why not use distributed transactions?

They would increase coupling across services. I wanted local consistency inside each service and explicit orchestration across services.

### Why split operator and customer UIs?

Because they serve different users, expose different capabilities, and will eventually need different security boundaries.

### What would you improve next?

- production-grade IAM
- richer read models
- stronger observability
- more formal migration/versioning strategy
