# Showcase Overview

## Purpose

This document presents the current repository as a technical showcase and explains what makes it worth discussing in an interview, portfolio review, or architecture conversation.

## What This Project Demonstrates

`BasicBankingSystem` is a banking platform prototype that combines:

- domain-oriented backend services
- gateway and BFF entry patterns
- asynchronous deposit workflow handling
- outbox, idempotency, and saga-style recovery
- separate operator, customer, and platform-facing frontend experiences
- layered testing and OpenAPI-based documentation

The project is intentionally small enough to understand end to end, but deep enough to show realistic distributed-system tradeoffs.

## Main User-Facing Surfaces

### Operations Console

Purpose:

- browse customers
- activate customers
- open accounts
- submit deposits and withdrawals
- inspect account activity
- work with pending-review items

Relevant source:

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`

### Customer Portal

Purpose:

- sign in with customer-facing credentials
- review balances and recent activity
- submit deposits and withdrawals
- track transaction status

Relevant source:

- `src/Banking.CustomerPortal/src/App.tsx`
- `src/Banking.CustomerPortal/src/api.ts`
- `src/Banking.Bff.CustomerPortal/`

### Platform Operations Console

Purpose:

- monitor service health and compatibility
- inspect workflow backlog and worker status
- trace correlation ids
- run controlled maintenance actions
- review platform operations audit events

Relevant source:

- `src/Banking.PlatformOps/src/App.tsx`
- `src/Banking.PlatformOps/src/api.ts`
- `src/Banking.Gateway/Controllers/PlatformController.cs`

## Main Backend Services

### Customer Service

Responsibilities:

- customer master data
- customer status lifecycle
- portal sign-in validation

### Account Service

Responsibilities:

- account creation
- balance ownership
- account lookup and activity history
- withdrawals

### Deposit Service

Responsibilities:

- deposit intake
- idempotency enforcement
- workflow state persistence
- outbox dispatch
- asynchronous processing
- compensation, retry, and pending review

### Audit Service

Responsibilities:

- audit persistence
- audit retrieval

### Gateway And BFF Layer

Important runtime roles:

- `Banking.Gateway` acts as the operator-facing entry point and platform control plane
- `Banking.Bff.CustomerPortal` acts as the customer-facing aggregation and session-auth backend

## Core Technical Themes

- `Microservices and bounded contexts`
- `Gateway + BFF entry architecture`
- `Outbox, idempotency, and saga-style recovery`
- `Operator UX, customer UX, and platform UX separation`
- `Layered automated testing`
- `OpenAPI and Swagger`

## Why This Is A Good Portfolio Project

This repository shows more than feature delivery. It shows:

- system decomposition
- workflow reliability design
- frontend/backend boundary design
- security boundary decisions
- operational recovery thinking
- testing and documentation discipline

That combination makes it a strong full-stack architecture showcase rather than a simple demo app.
