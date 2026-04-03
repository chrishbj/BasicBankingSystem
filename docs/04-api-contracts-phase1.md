# Phase 1 API Contracts

## Common Conventions

- Base path: `/api/v1`
- Auth: `X-Api-Key` for documented public service APIs
- Tracing: `X-Correlation-Id`
- Idempotency: `Idempotency-Key` on critical write APIs
- Error model: `ProblemDetails`-style JSON
- Pagination:
  - `pageNumber`
  - `pageSize`
  - `items`
  - `totalCount`
  - `totalPages`

## Contract Scope

This phase 1 contract currently covers the documented public backend service APIs.

It does not currently include:

- Gateway routes
- Customer Portal BFF routes
- demo-only endpoints
- internal-only service-to-service endpoints

Those boundaries are currently protected by integration tests unless they are explicitly promoted into a contract document.

## Customer APIs

- `POST /api/v1/customers`
- `GET /api/v1/customers`
- `GET /api/v1/customers/{customerId}`
- `POST /api/v1/customers/portal-sign-in`
- `POST /api/v1/customers/{customerId}/status`

## Account APIs

- `POST /api/v1/accounts`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{accountId}`
- `GET /api/v1/accounts/by-number/{accountNumber}`
- `POST /api/v1/accounts/{accountId}/withdrawals`
- `GET /api/v1/accounts/{accountId}/activities`

## Deposit APIs

- `POST /api/v1/deposits`
- `GET /api/v1/deposits`
- `GET /api/v1/deposits/{transactionId}`
- `GET /api/v1/deposits/review/pending`
- `POST /api/v1/deposits/{transactionId}/review/retry-compensation`
- `POST /api/v1/deposits/{transactionId}/review/resolve`

## Audit APIs

- `GET /api/v1/audits`
- `GET /api/v1/audits/{auditId}`

## Platform APIs

- `GET /api/v1/health`
- `GET /api/v1/ready`

## Reference

The formal contract is defined in [openapi-phase1.yaml](/E:/DemoProjects/BasicBankingSystem/docs/openapi-phase1.yaml).
