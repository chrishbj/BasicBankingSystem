# Backend Technical Guide

## Purpose

This document explains the backend architecture, main technologies, important design patterns, and the core technical tradeoffs behind the current banking prototype.

## Scope

The backend currently includes:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`
- `Gateway`
- shared building blocks

## Technology Stack

- `.NET 10`
- `ASP.NET Core Web API`
- `Entity Framework Core`
- `PostgreSQL`
- `RabbitMQ`
- `xUnit`
- `FluentAssertions`
- `WebApplicationFactory`
- `Docker Compose`

## Service Responsibilities

### Customer Service

- customer master data
- customer status lifecycle

### Account Service

- account lifecycle
- balance ownership
- account activity history

### Deposit Service

- deposit intake
- idempotency handling
- transaction state management
- pending-review and compensation flow

### Audit Service

- audit persistence
- audit retrieval

## Important Design Patterns

### Microservice Boundary by Domain

The backend is split by responsibility, not by technical layer.

Why:

- clearer ownership
- fewer cross-domain side effects
- better path to scale and independent evolution

### SAGA for Cross-Service Transaction Coordination

Deposits are long-running workflows that touch multiple services.

The current design uses saga-style coordination for:

- initial deposit request
- account posting
- audit write
- compensation or review when something fails

Why not distributed transactions:

- too tightly coupled
- harder to operate
- less resilient in cloud-style environments

### Outbox Pattern

`Deposit Service` persists transaction state and outbound integration messages before dispatch.

Why:

- reduces the risk of “saved in database but event not published”
- improves recovery and retry behavior

### Idempotency Pattern

Critical write paths use idempotency to prevent duplicate financial effects.

This is especially important for:

- deposit submission
- compensation handling
- posting references

### Backend-for-Operations Model

The backend is shaped for operator workflows, not only CRUD storage.

Examples:

- account activity history
- pending-review queries
- retry and manual resolution operations

## Key Technical Decisions and Tradeoffs

### Keep Balance Ownership in Account Service

The account service is the source of truth for balance updates.

Tradeoff:

- more service-to-service calls
- but much clearer balance ownership and consistency boundaries

### Keep Audit Independent

Audit is intentionally separated from the main transaction services.

Tradeoff:

- more moving parts
- but better compliance isolation and clearer retry behavior

### Prefer Local Transactions Plus Messaging

Each service uses local persistence guarantees, while cross-service consistency is eventual.

Tradeoff:

- more status handling and operational complexity
- but better scalability and fault isolation

### Support Human-Facing Numbers Alongside Internal IDs

The system keeps internal ids for machine boundaries, while exposing:

- `customerNumber`
- `accountNumber`
- `transactionNumber`
- operator `referenceNumber`

Tradeoff:

- more identifiers to maintain
- but much better operational usability

## Testing Strategy

The backend uses layered testing:

- unit tests for service logic
- integration tests for HTTP and persistence behavior
- contract-oriented checks for API expectations

This keeps changes safer in areas such as:

- balance mutation
- deposit status transitions
- compensation handling
- account-number lookups

## Current Limitations

- no centralized query/read-model service yet
- no full production-grade migration system yet
- authorization is still coarse compared with real banking roles
- observability is good for local development, but not yet complete for production operations

## Recommended Next Steps

- add richer read-model aggregation for customer-level activity
- strengthen role-based authorization
- formalize migrations and data versioning strategy
- expand resilience and monitoring around RabbitMQ and retry paths
