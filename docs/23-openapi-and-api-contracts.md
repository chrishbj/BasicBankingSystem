# OpenAPI And API Contracts

## Why OpenAPI Matters In This Project

This repository uses OpenAPI as both documentation and a stability mechanism between backend services, the operations console, and the customer portal.

## Main OpenAPI Assets

- `docs/openapi-phase1.yaml`
- `tests/Banking.Contracts.Tests/OpenApiContractTests.cs`

## Runtime Swagger Support

Each backend service exposes Swagger UI in local development and Docker mode.

Examples:

- `src/Banking.Services.Customer/Program.cs`
- `src/Banking.Services.Account/Program.cs`
- `src/Banking.Services.Deposit/Program.cs`
- `src/Banking.Services.Audit/Program.cs`

The shared header documentation is injected through:

- `src/Banking.BuildingBlocks/Swagger/BankingSecurityHeadersOperationFilter.cs`

This makes local testing clearer because required headers appear directly in Swagger:

- `X-Api-Key`
- `X-Correlation-Id`
- `Idempotency-Key`
- internal service identity headers where relevant

## How Contracts Help The Frontends

The React applications depend on stable API shapes for:

- customer lookup
- account lookup by account number
- account activity history
- deposit search and transaction status
- pending review workflows

Frontend contract usage:

- `src/Banking.Web/src/types.ts`
- `src/Banking.Web/src/api.ts`
- `src/Banking.CustomerPortal/src/types.ts`
- `src/Banking.CustomerPortal/src/api.ts`

## Contract-First Benefit

Even though the repository evolved iteratively, the OpenAPI document acts like a contract anchor:

- easier manual validation through Swagger
- easier automated validation through tests
- easier frontend integration
- easier portfolio explanation because the API surface is explicit

## Tradeoffs

Benefits:

- clearer service surface
- faster local testing
- better integration discipline

Costs:

- contracts must be updated alongside code
- implementation can move faster than documentation if not maintained carefully

This project intentionally treats that maintenance work as part of the engineering process, not as optional polish.
