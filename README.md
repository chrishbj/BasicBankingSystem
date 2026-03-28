# BasicBankingSystem

A backend-first basic banking system prototype designed for phased evolution from a local development skeleton into a scalable, event-driven platform.

The current repository focuses on Phase 1:

- Customer management
- Account opening
- Deposit processing
- Audit logging
- TDD-friendly backend architecture

## Current Status

This repository currently provides:

- A backend-only .NET 10 solution
- Separate services for `Customer`, `Account`, `Deposit`, and `Audit`
- A lightweight `Gateway`
- Shared building blocks for API defaults and correlation handling
- Unit tests, integration tests, and contract tests
- English project documentation in `docs/`
- Chinese project documentation in `docs/ch-cn/` (not pushed to GitHub)

## Tech Stack

- `.NET 10`
- `ASP.NET Core Web API`
- `xUnit`
- `FluentAssertions`
- `WebApplicationFactory`
- OpenAPI-first documentation

## Solution Structure

```text
BasicBankingSystem/
  docs/
  src/
    Banking.Gateway/
    Banking.BuildingBlocks/
    Banking.Services.Customer/
    Banking.Services.Account/
    Banking.Services.Deposit/
    Banking.Services.Audit/
  tests/
    Banking.Contracts.Tests/
    Banking.Services.Customer.UnitTests/
    Banking.Services.Customer.IntegrationTests/
    Banking.Services.Account.UnitTests/
    Banking.Services.Account.IntegrationTests/
    Banking.Services.Deposit.UnitTests/
    Banking.Services.Deposit.IntegrationTests/
    Banking.Services.Audit.UnitTests/
    Banking.Services.Audit.IntegrationTests/
```

## Implemented APIs

### Customer Service

- `POST /api/v1/customers`
- `GET /api/v1/customers/{customerId}`
- `GET /api/v1/customers`
- `POST /api/v1/customers/{customerId}/status`

### Account Service

- `POST /api/v1/accounts`
- `GET /api/v1/accounts/{accountId}`
- `GET /api/v1/accounts`

### Deposit Service

- `POST /api/v1/deposits`
- `GET /api/v1/deposits/{transactionId}`
- `GET /api/v1/deposits`

### Audit Service

- `POST /api/v1/audits`
- `GET /api/v1/audits/{auditId}`
- `GET /api/v1/audits`

## Quick Start

### Prerequisites

- .NET SDK 10
- PowerShell

### Run All Tests

```powershell
dotnet test BasicBankingSystem.slnx
```

### Run Services Locally

```powershell
dotnet run --project src/Banking.Gateway
dotnet run --project src/Banking.Services.Customer
dotnet run --project src/Banking.Services.Account
dotnet run --project src/Banking.Services.Deposit
dotnet run --project src/Banking.Services.Audit
```

Default local ports:

- `Gateway`: `http://localhost:5000`
- `Customer`: `http://localhost:5101`
- `Account`: `http://localhost:5102`
- `Deposit`: `http://localhost:5103`
- `Audit`: `http://localhost:5104`

For full local run, test, and publish instructions, see [docs/10-local-run-and-test.md](docs/10-local-run-and-test.md).

## Architecture Direction

The project is intentionally evolving toward:

- bounded-context-oriented services
- reliable transaction processing
- idempotent write APIs
- eventual consistency with SAGA
- auditability and observability

The current implementation is still a local development skeleton using in-memory persistence in order to keep iteration speed high. The next planned steps are:

- replace in-memory stores with local infrastructure
- introduce message-driven flows
- evolve deposit processing toward Outbox and SAGA orchestration

## Documentation

### English Docs

- [Requirements](docs/01-requirements.md)
- [Architecture Draft](docs/02-architecture.md)
- [User Stories and Acceptance](docs/03-user-stories-and-acceptance.md)
- [API Contracts](docs/04-api-contracts-phase1.md)
- [OpenAPI](docs/openapi-phase1.yaml)
- [Testing Strategy and TDD](docs/05-testing-strategy-and-tdd.md)
- [TDD Backlog](docs/06-tdd-backlog-phase1.md)
- [Core Diagrams](docs/07-core-diagrams.md)
- [Architecture Review](docs/08-architecture-review.md)
- [Architecture Review PPT Outline](docs/09-architecture-review-ppt-outline.md)
- [Local Run and Test Guide](docs/10-local-run-and-test.md)

### Chinese Docs

Chinese documents are maintained locally under `docs/ch-cn/` and are intentionally excluded from the GitHub repository.

## Development Approach

The repository is being built with a TDD-oriented workflow:

- define contract and behavior first
- write tests before or alongside implementation
- keep services small and independently verifiable
- evolve infrastructure only after the business slices are stable

## Next Recommended Step

- Add local infrastructure with Docker Compose
- Introduce PostgreSQL and RabbitMQ locally
- Evolve `Deposit` toward Outbox and SAGA
