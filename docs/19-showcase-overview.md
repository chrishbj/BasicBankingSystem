# Showcase Overview

## Purpose

This document presents the project as a personal technical showcase. It explains what the system does, why it is technically interesting, and where the most important implementation points live in the source tree.

## What This Project Demonstrates

`BasicBankingSystem` is a local-first banking platform prototype with:

- a microservice-oriented backend
- asynchronous transaction processing
- SAGA-style compensation and review handling
- idempotent write APIs for financial safety
- operator and customer-facing frontends
- layered automated testing
- OpenAPI-driven documentation and local Swagger environments

The project is intentionally small enough to understand end-to-end, while still showing the patterns used in more serious distributed systems.

## Main User-Facing Surfaces

### Operations Console

Purpose:

- browse and select customers
- open accounts
- submit deposits and withdrawals
- inspect account activity
- handle pending-review items

Relevant source:

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`
- `src/Banking.Web/src/components/CustomerPanel.tsx`
- `src/Banking.Web/src/components/AccountPanel.tsx`
- `src/Banking.Web/src/components/PendingReviewPanel.tsx`

### Customer Portal

Purpose:

- sign in with customer-facing credentials
- review balances and activity
- submit deposits and withdrawals
- track deposit processing state

Relevant source:

- `src/Banking.CustomerPortal/src/App.tsx`
- `src/Banking.CustomerPortal/src/api.ts`

## Main Backend Services

### Customer Service

Responsibilities:

- customer master data
- status lifecycle
- demo portal sign-in validation

Relevant source:

- `src/Banking.Services.Customer/Program.cs`
- `src/Banking.Services.Customer/Controllers/CustomersController.cs`
- `src/Banking.Services.Customer/Services/CustomerService.cs`

### Account Service

Responsibilities:

- account creation
- account-number lookup
- balance ownership
- account activity history
- withdrawal processing

Relevant source:

- `src/Banking.Services.Account/Program.cs`
- `src/Banking.Services.Account/Controllers/AccountsController.cs`
- `src/Banking.Services.Account/Services/AccountService.cs`

### Deposit Service

Responsibilities:

- deposit intake
- idempotency enforcement
- outbox persistence
- RabbitMQ publishing and consumption
- SAGA step tracking
- pending-review recovery

Relevant source:

- `src/Banking.Services.Deposit/Program.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

### Audit Service

Responsibilities:

- immutable-style audit persistence
- audit retrieval for troubleshooting and compliance

Relevant source:

- `src/Banking.Services.Audit/Program.cs`
- `src/Banking.Services.Audit/Controllers/AuditsController.cs`

## Core Technical Themes

- `Microservices and bounded contexts`
- `SAGA and compensation`
- `Idempotency and Outbox`
- `Automated testing`
- `OpenAPI and Swagger`
- `Human-friendly identifiers for operational UX`

Related deep-dive documents:

- [Microservices And Boundaries](20-microservices-and-boundaries.md)
- [Saga, Outbox, And Idempotency](21-saga-outbox-idempotency.md)
- [Testing And Quality](22-testing-and-quality.md)
- [OpenAPI And API Contracts](23-openapi-and-api-contracts.md)

## Why This Is A Good Portfolio Project

This repository shows more than CRUD:

- asynchronous messaging
- resilient transaction handling
- compensating actions
- internal and external authentication boundaries
- customer-facing and operator-facing UI separation
- documentation and testing discipline

It is a useful demonstration of system design, backend architecture, frontend integration, and engineering communication in one codebase.
