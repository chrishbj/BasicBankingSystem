# One-Page Showcase Summary

## Project

`BasicBankingSystem` is a banking platform prototype built to showcase distributed backend design, transaction safety patterns, gateway/BFF entry architecture, and full-stack engineering discipline.

## What It Demonstrates

- domain-oriented backend services
- `Gateway + BFF` entry patterns
- idempotent financial write APIs
- outbox-based async workflow startup
- saga-style compensation and pending-review recovery
- separate operator, customer, and platform frontend experiences
- layered automated testing and OpenAPI documentation

## Core Product Surfaces

### Operations Console

- browse and activate customers
- open accounts
- submit deposits and withdrawals
- inspect account activity
- handle pending-review items

### Customer Portal

- sign in with customer-facing credentials
- review balances and account activity
- submit deposits and withdrawals
- track transaction status

### Platform Operations Console

- monitor service and workflow status
- inspect outbox and pending-review backlog
- run platform maintenance actions
- trace correlations and review platform audit output

## Core Backend Shape

The backend is split into:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`
- `Banking.Gateway`
- `Banking.Bff.CustomerPortal`

## Core Technical Highlights

### Clear Ownership Boundaries

- `Account Service` owns balances
- `Deposit Service` owns deposit workflow state
- `Audit Service` owns audit persistence
- `Gateway` and `BFF` own different entry-layer concerns

### Transaction Safety

The deposit flow demonstrates:

- `Idempotency-Key`
- persisted outbox messages
- asynchronous message processing
- saga-style compensation
- `PendingReview` fallback and retry

### Multi-Surface Full-Stack Design

The repository includes:

- an operations console for business workflows
- a customer portal for self-service flows
- a platform console for diagnostics and maintenance

### Testing Discipline

The project includes:

- unit tests
- integration tests with `WebApplicationFactory`
- OpenAPI contract tests
- Newman and smoke-regression assets

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

This repository shows:

- system decomposition
- distributed workflow reliability
- frontend/backend boundary design
- operational and customer UX thinking
- testing and documentation maturity

## Recommended Talking Points

1. Why balances belong to `Account Service`
2. Why deposits use `Idempotency + Outbox + Saga-style recovery`
3. Why `Gateway` and `BFF` are separate
4. Why the project has three frontend surfaces instead of one
5. How testing and documentation support safe iteration across multiple services
