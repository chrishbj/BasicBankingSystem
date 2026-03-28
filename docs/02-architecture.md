# Basic Banking System Architecture Draft

## Baseline

Keep the strengths of `ReactFullStackDemo`:

- React frontend
- ASP.NET Core services
- Docker-based local development
- Cloud-ready deployment path

But evolve the backend from a single API into a microservice-oriented banking platform.

## Proposed Architecture

- Frontend: `React + Vite`
- Entry point: `API Gateway / BFF`
- Services:
  - `Customer Service`
  - `Account Service`
  - `Deposit Service`
  - `Audit Service`
  - `Query Service`
- Messaging: message bus
- Observability: logs, metrics, tracing

## Service Boundaries

### Customer Service

- Customer master data
- Customer status
- Uniqueness validation

### Account Service

- Account lifecycle
- Balance ownership
- Account-level concurrency control

### Deposit Service

- Deposit intake
- Idempotency
- Transaction lifecycle

### Audit Service

- Audit ingestion
- Audit search
- Retry and compensation for audit failures

### Query Service

- Aggregated read models
- Read-optimized APIs

## Data Recommendations

Do not rely on a single Mongo-style CRUD store for core ledger behavior.

Recommended direction:

- PostgreSQL or SQL Server for `Customer`, `Account`, and `Deposit`
- Append-only or relational storage for `Audit`
- Read-optimized store for `Query`
- Redis for caching and support utilities

## Consistency Model

- Local transaction inside each service
- No distributed 2PC
- SAGA for cross-service workflows
- Outbox for reliable event publication
- Idempotent consumers

## Deposit Flow Summary

1. Deposit Service accepts request and creates a transaction
2. Deposit Service publishes `DepositRequested`
3. Account Service validates and posts balance
4. Account Service publishes `DepositPosted` or `DepositRejected`
5. Deposit Service updates transaction state
6. Audit Service writes audit record
7. Query Service updates read model

## Suggested Monorepo Layout

```text
BasicBankingSystem/
  docs/
  infra/
  src/
    Banking.Web/
    Banking.Gateway/
    Banking.Services.Customer/
    Banking.Services.Account/
    Banking.Services.Deposit/
    Banking.Services.Audit/
    Banking.Services.Query/
    Banking.BuildingBlocks/
  tests/
```
