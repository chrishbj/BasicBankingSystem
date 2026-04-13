# Showcase Talk Track

## Purpose

This is a short presentation script for introducing the project in an interview, portfolio review, or technical discussion.

## 30-Second Version

`BasicBankingSystem` is a banking platform prototype I built to demonstrate domain-oriented microservices, gateway and BFF entry architecture, idempotent financial APIs, outbox- and saga-style deposit handling, and three separate frontend experiences for operators, customers, and platform operations.

## 2-3 Minute Version

### 1. Problem Framing

I wanted a project that was more realistic than CRUD, but still small enough to understand end to end. Banking is a good fit because even a basic deposit workflow forces you to deal with ownership, transaction safety, retries, compensation, and operational visibility.

### 2. Architecture

I split the backend into:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

On top of that, I added:

- `Banking.Gateway` for operator-facing entry and platform control
- `Banking.Bff.CustomerPortal` for customer-facing session-based flows

The most important design choice is that balances stay in `Account Service`, while `Deposit Service` coordinates the cross-service workflow.

### 3. Transaction Handling

The deposit flow is intentionally not a simple synchronous write. It demonstrates:

- `Idempotency-Key` handling
- persisted outbox records
- asynchronous dispatch and consumption
- saga-style compensation
- `PendingReview` and retry when automation cannot finish safely

### 4. Frontend Surfaces

I built three different frontend surfaces:

- an operations console for business workflows
- a customer portal for self-service flows
- a platform operations console for monitoring, diagnostics, and maintenance

That separation makes the trust boundaries and user responsibilities much clearer.

### 5. Quality And Documentation

I also treated testing and documentation as first-class engineering work:

- unit tests
- integration tests
- OpenAPI contract checks
- Swagger support
- Newman regression assets
- architecture and implementation writeups

## Likely Interview Questions

### Why use microservices here?

To make ownership boundaries, eventual consistency, and workflow orchestration explicit.

### Why not use distributed transactions?

Because I wanted local consistency inside each service and explicit recovery across services.

### Why split operator, customer, and platform experiences?

Because they have different users, responsibilities, and security expectations.

### What would you improve next?

- stronger IAM for the customer portal
- tighter authorization on some maintenance endpoints
- richer observability and alerting
- more formal schema migration/versioning discipline
