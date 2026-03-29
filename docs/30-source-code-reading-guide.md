# Source Code Reading Guide

## Purpose

This guide is for readers who want to understand the codebase efficiently without reading every file in order.

It suggests a practical reading path through the most important backend, frontend, and testing files.

## Recommended Reading Order

## 1. Start With The Big Picture

Read these first:

- `README.md`
- `docs/19-showcase-overview.md`
- `docs/20-microservices-and-boundaries.md`
- `docs/29-database-schema-and-relationships.md`

Why:

- they explain the system shape before you dive into code

## 2. Understand The Deposit Workflow First

If you want to understand the most technically interesting part of the repository, start here:

- `src/Banking.Services.Deposit/Program.cs`
- `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

Why:

- this is where the project demonstrates `Idempotency`, `Outbox`, `SAGA`, and `PendingReview`

## 3. Then Read Account Service

Next, read:

- `src/Banking.Services.Account/Program.cs`
- `src/Banking.Services.Account/Controllers/AccountsController.cs`
- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.Services.Account/Data/AccountDbContext.cs`

Why:

- `Account Service` is the single owner of balances
- it shows how deposits and withdrawals become account postings

## 4. Then Read Customer Service

Recommended files:

- `src/Banking.Services.Customer/Program.cs`
- `src/Banking.Services.Customer/Controllers/CustomersController.cs`
- `src/Banking.Services.Customer/Services/CustomerService.cs`
- `src/Banking.Services.Customer/Data/CustomerDbContext.cs`

Why:

- it is simpler than `Deposit Service`
- it shows the shared startup model and the portal sign-in flow

## 5. Read Shared Building Blocks

These files explain cross-cutting behavior used by every service:

- `src/Banking.BuildingBlocks/Extensions/BankingServiceCollectionExtensions.cs`
- `src/Banking.BuildingBlocks/Security/BankingHeaderAuthenticationHandler.cs`
- `src/Banking.BuildingBlocks/Security/InternalServiceAuthenticationDelegatingHandler.cs`
- `src/Banking.BuildingBlocks/Swagger/BankingSecurityHeadersOperationFilter.cs`

Why:

- they explain authentication, internal service identity, ProblemDetails, and Swagger header documentation

## 6. Read The Frontends

### Operations Console

Start here:

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`
- `src/Banking.Web/src/components/CustomerPanel.tsx`
- `src/Banking.Web/src/components/AccountPanel.tsx`
- `src/Banking.Web/src/components/PendingReviewPanel.tsx`

Why:

- these show how operator workflows are modeled around selected customer and account context

### Customer Portal

Start here:

- `src/Banking.CustomerPortal/src/App.tsx`
- `src/Banking.CustomerPortal/src/api.ts`

Why:

- these show the customer-facing version of the same backend capabilities

## 7. Read The Tests

To understand what the project considers important, read the tests next:

- `tests/Banking.Services.Deposit.UnitTests/DepositTransactionProcessorTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositServiceTests.cs`
- `tests/Banking.Services.Deposit.IntegrationTests/DepositsApiTests.cs`
- `tests/Banking.Services.Account.UnitTests/AccountServiceTests.cs`
- `tests/Banking.Services.Customer.IntegrationTests/CustomersApiTests.cs`
- `tests/Banking.Contracts.Tests/OpenApiContractTests.cs`

Why:

- the tests reveal the intended business rules and failure handling

## 8. Finally, Read The OpenAPI Contract

Read:

- `docs/openapi-phase1.yaml`

Why:

- it gives you the public contract shape after you already understand the internal implementation

## Best Paths By Interest

### If You Care About Distributed Transactions

Read in this order:

1. `src/Banking.Services.Deposit/Services/DepositService.cs`
2. `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
3. `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
4. `src/Banking.Services.Account/Services/AccountService.cs`
5. `tests/Banking.Services.Deposit.UnitTests/DepositTransactionProcessorTests.cs`

### If You Care About API Design

Read in this order:

1. `docs/openapi-phase1.yaml`
2. `src/Banking.Services.Customer/Controllers/CustomersController.cs`
3. `src/Banking.Services.Account/Controllers/AccountsController.cs`
4. `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
5. `src/Banking.BuildingBlocks/Swagger/BankingSecurityHeadersOperationFilter.cs`

### If You Care About Frontend Architecture

Read in this order:

1. `src/Banking.Web/src/App.tsx`
2. `src/Banking.Web/src/hooks/useOperationsConsole.ts`
3. `src/Banking.CustomerPortal/src/App.tsx`
4. `src/Banking.Web/src/api.ts`
5. `src/Banking.CustomerPortal/src/api.ts`

## Summary

If you only read a small part of the repository, the best high-value set is:

- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.BuildingBlocks/Extensions/BankingServiceCollectionExtensions.cs`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`
- `tests/Banking.Services.Deposit.UnitTests/DepositTransactionProcessorTests.cs`

Those files capture most of the architectural ideas in the project.
