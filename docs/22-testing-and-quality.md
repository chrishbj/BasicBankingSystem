# Testing And Quality

## Testing Philosophy

This project uses layered testing so that domain logic, HTTP behavior, and contract expectations can all be verified independently.

For the repository-wide design rules used for new tests, see:

- `docs/33-test-design-standards.md`
- `docs/34-testing-roadmap.md`
- `docs/35-test-design-findings-and-remedies.md`

## Test Stack

- `xUnit`
- `FluentAssertions`
- `Moq`
- `WebApplicationFactory`
- shared SQLite-backed integration environments for service APIs
- Postman and Newman for regression runs

## Test Layers

### Unit Tests

Used for:

- service logic
- transition rules
- idempotency behavior
- saga and compensation paths
- dependency side effects

Examples:

- `tests/Banking.Services.Customer.UnitTests/CustomerServiceTests.cs`
- `tests/Banking.Services.Account.UnitTests/AccountServiceTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositServiceTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositTransactionProcessorTests.cs`
- `tests/Banking.Services.Deposit.UnitTests/DepositOutboxDispatcherTests.cs`
- `tests/Banking.Services.Audit.UnitTests/AuditServiceTests.cs`

Unit tests in the current repository standard use `Moq` to isolate repositories, publishers, directories, and other service dependencies. Reusable builders and fixture data live in per-project `Support/` folders.

### Integration Tests

Used for:

- real HTTP endpoints
- model binding and status codes
- persistence behavior
- API-level regression protection
- proxy and BFF boundary behavior

Examples:

- `tests/Banking.Services.Customer.IntegrationTests/CustomersApiTests.cs`
- `tests/Banking.Services.Account.IntegrationTests/AccountsApiTests.cs`
- `tests/Banking.Services.Deposit.IntegrationTests/DepositsApiTests.cs`
- `tests/Banking.Services.Audit.IntegrationTests/AuditsApiTests.cs`
- `tests/Banking.Gateway.IntegrationTests/GatewayApiTests.cs`
- `tests/Banking.Bff.CustomerPortal.IntegrationTests/CustomerPortalBffApiTests.cs`

The service integration test setup uses a shared SQLite `WebApplicationFactory` base in `tests/Shared/SqliteWebApplicationFactory.cs`, with service-specific factories inheriting from it. Local `Support/` helpers are used for request builders, unique data generation, and async drivers where needed.

Current integration tests focus on:

- status codes and payload contracts
- `ProblemDetails` error responses
- pagination metadata and out-of-range pages
- filtering and sorting behavior
- cross-boundary authorization and session behavior

### Contract Tests

Used for:

- validating that the OpenAPI contract stays aligned with the intended public service API surface
- detecting drift between documented paths and implemented service endpoints
- protecting shared schemas such as `ProblemDetails` and paged responses

Example:

- `tests/Banking.Contracts.Tests/OpenApiContractTests.cs`

Current contract-test boundary:

- `docs/openapi-phase1.yaml` is the checked contract source for the documented backend service APIs
- Gateway and Customer Portal BFF are currently protected by integration tests rather than this contract document
- contract tests should use structural OpenAPI assertions, not broad string-matching checks

## Why This Testing Structure Matters

It demonstrates that the repository is not only about writing features, but about keeping behavior stable while architecture evolves.

That is especially important here because the codebase contains:

- asynchronous processing
- compensation logic
- multiple services
- frontends and BFFs that depend on stable contracts
- proxy boundaries that must preserve downstream behavior

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
