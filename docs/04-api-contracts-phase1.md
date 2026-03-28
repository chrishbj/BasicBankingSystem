# Phase 1 API Contracts

## Common Conventions

- Base path: `/api/v1`
- Auth: `Authorization: Bearer <token>`
- Tracing: `X-Correlation-Id`
- Idempotency: `Idempotency-Key` on critical write APIs
- Error model: ProblemDetails-style JSON
- Pagination:
  - `pageNumber`
  - `pageSize`
  - `items`
  - `totalCount`
  - `totalPages`

## Customer APIs

- `POST /api/v1/customers`
- `GET /api/v1/customers/{customerId}`
- `GET /api/v1/customers`
- `PUT /api/v1/customers/{customerId}/contact`
- `POST /api/v1/customers/{customerId}/status`

## Account APIs

- `POST /api/v1/accounts`
- `GET /api/v1/accounts/{accountId}`
- `GET /api/v1/accounts`
- `POST /api/v1/accounts/{accountId}/close`

## Deposit APIs

- `POST /api/v1/deposits`
- `GET /api/v1/deposits/{transactionId}`
- `GET /api/v1/deposits`
- `GET /api/v1/deposits/by-number/{transactionNumber}`

## Audit APIs

- `GET /api/v1/audits`
- `GET /api/v1/audits/{auditId}`

## Platform APIs

- `GET /api/v1/health`
- `GET /api/v1/ready`

## Reference

The formal contract is defined in [openapi-phase1.yaml](/E:/DemoProjects/BasicBankingSystem/docs/openapi-phase1.yaml).
