# Testing And Quality

## Testing Philosophy

This project uses layered testing so that domain logic, HTTP behavior, and contract expectations can all be verified independently.

## Test Stack

- `xUnit`
- `FluentAssertions`
- `WebApplicationFactory`
- SQLite-backed integration environments

## Test Layers

### Unit Tests

Used for:

- service logic
- transition rules
- idempotency behavior
- saga and compensation paths

Examples:

- `tests/Banking.Services.Customer.UnitTests/CustomerServiceTests.cs`
- `tests/Banking.Services.Account.UnitTests/AccountServiceTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositServiceTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositTransactionProcessorTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositOutboxDispatcherTests.cs`

### Integration Tests

Used for:

- real HTTP endpoints
- model binding and status codes
- persistence behavior
- API-level regression protection

Examples:

- `tests/Banking.Services.Customer.IntegrationTests/CustomersApiTests.cs`
- `tests/Banking.Services.Account.IntegrationTests/AccountsApiTests.cs`
- `tests/Banking.Services.Deposit.IntegrationTests/DepositsApiTests.cs`
- `tests/Banking.Services.Audit.IntegrationTests/AuditsApiTests.cs`

The integration test setup uses `WebApplicationFactory` per service.

Examples:

- `tests/Banking.Services.Customer.IntegrationTests/CustomerServiceWebApplicationFactory.cs`
- `tests/Banking.Services.Deposit.IntegrationTests/DepositServiceWebApplicationFactory.cs`

### Contract Tests

Used for:

- validating the OpenAPI contract stays aligned with the intended MVP surface

Example:

- `tests/Banking.Contracts.Tests/OpenApiContractTests.cs`

## Why This Testing Structure Matters

It demonstrates that the repository is not only about writing features, but about keeping behavior stable while architecture evolves.

That is especially important here because the codebase contains:

- asynchronous processing
- compensation logic
- multiple services
- frontends that depend on stable contracts

## Practical Quality Controls

- buildable frontends with Vite production builds
- local Swagger for manual API verification
- Postman and Newman regression assets
- Docker Desktop stack for end-to-end local validation

Related source and assets:

- `postman/BasicBankingSystem-Local.postman_collection.json`
- `scripts/manual-smoke-test.ps1`
- `scripts/run-newman-local.ps1`
- `.github/workflows/newman-local-regression.yml`
