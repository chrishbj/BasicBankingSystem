# One-Page Showcase Summary

## Project

`BasicBankingSystem` is a local-first banking platform prototype built to showcase distributed backend design, transaction safety patterns, and full-stack engineering discipline.

## What It Demonstrates

- domain-oriented microservices
- SAGA-style transaction coordination
- Outbox-based event publishing
- idempotent financial write APIs
- operator-facing and customer-facing React applications
- layered automated testing
- OpenAPI and Swagger-driven integration

## Core Business Flows

### Operator Flow

- browse customers
- open accounts
- submit deposits and withdrawals
- inspect account activity
- resolve pending-review items

Relevant source:

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`

### Customer Flow

- sign in with customer-facing credentials
- review balances and activity
- submit deposits and withdrawals
- track transaction status

Relevant source:

- `src/Banking.CustomerPortal/src/App.tsx`
- `src/Banking.CustomerPortal/src/api.ts`

## Core Technical Highlights

### Microservices

The backend is split into:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

This keeps ownership clear and makes cross-service workflow design visible.

### Transaction Safety

The deposit flow demonstrates:

- `Idempotency-Key` protection
- persisted outbox messages
- asynchronous RabbitMQ processing
- compensation and `PendingReview` handling

Relevant source:

- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`

### Testing Discipline

The project includes:

- unit tests
- integration tests with `WebApplicationFactory`
- OpenAPI contract tests
- manual smoke-test scripts
- Newman regression assets

Relevant source:

- `tests/Banking.Services.Deposit.UnitTests/`
- `tests/Banking.Services.Deposit.IntegrationTests/`
- `tests/Banking.Contracts.Tests/OpenApiContractTests.cs`

## Tech Stack

- `.NET 10`
- `ASP.NET Core`
- `Entity Framework Core`
- `PostgreSQL`
- `RabbitMQ`
- `React 19`
- `TypeScript`
- `Vite`
- `Docker Compose`
- `xUnit`
- `FluentAssertions`

## Why This Project Is Portfolio-Worthy

This repository shows more than feature implementation. It shows:

- system decomposition
- distributed workflow thinking
- resilience patterns
- operational UX design
- customer UX design
- testing and documentation maturity

## Recommended Talking Points

1. Why balances belong to `Account Service`
2. Why deposits use `Idempotency + Outbox + SAGA`
3. Why audit is isolated from transaction execution
4. Why the operator console and customer portal are separate frontends
5. How testing supports safe iteration across multiple services
