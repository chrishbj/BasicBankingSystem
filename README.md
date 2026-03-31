# BasicBankingSystem

A portfolio-ready banking platform prototype that demonstrates microservices, idempotent financial APIs, SAGA-style transaction handling, OpenAPI-driven integration, and full-stack delivery with separate operator and customer experiences.

## Why This Project Matters

This repository was built as a technical showcase rather than a simple CRUD demo.

It demonstrates:

- domain-oriented microservice boundaries
- resilient deposit processing with `Idempotency-Key`, `Outbox`, and `SAGA`
- explicit compensation and `PendingReview` recovery
- separate React applications for bank staff and customers
- layered testing with unit, integration, and contract coverage
- OpenAPI and Swagger as first-class integration surfaces

## Live Local Surfaces

When the Docker Desktop stack is running, the main entry points are:

- `Operations Console`: `http://localhost:18090`
- `Customer Portal`: `http://localhost:18091`
- `Customer Swagger`: `http://localhost:18081/swagger`
- `Account Swagger`: `http://localhost:18082/swagger`
- `Deposit Swagger`: `http://localhost:18083/swagger`
- `Audit Swagger`: `http://localhost:18084/swagger`

The recommended Docker Desktop port set is defined in [docker.env.local](/E:/DemoProjects/BasicBankingSystem/infra/docker.env.local). If you need different host ports, adjust that file or use [\.env.example](/E:/DemoProjects/BasicBankingSystem/infra/.env.example) as a template.

Customer portal demo sign-in uses:

- `Customer Number`
- `Identity Last 4 Digits`

Example:

- stored identity `WITHDRAW-DEMO-001`
- sign-in input `0001`

## Core System Shape

### Backend Services

- `Customer Service`: customer master data and portal sign-in validation
- `Account Service`: account lifecycle, balance ownership, and account activity
- `Deposit Service`: idempotent deposit intake, outbox dispatch, async processing, and compensation
- `Audit Service`: audit persistence and retrieval

### Frontends

- `Banking.Web`: operator-facing operations console
- `Banking.CustomerPortal`: customer-facing self-service portal

### Shared Infrastructure

- `PostgreSQL`
- `RabbitMQ`
- `Docker Compose`
- `Swagger / OpenAPI`

## Technical Highlights

### Microservices By Business Responsibility

The backend is intentionally split by domain rather than by technical layer. This makes ownership and workflow boundaries explicit.

Relevant source:

- `src/Banking.Services.Customer/`
- `src/Banking.Services.Account/`
- `src/Banking.Services.Deposit/`
- `src/Banking.Services.Audit/`

### Idempotent Financial Writes

Deposits require `Idempotency-Key`, and downstream posting references are also treated as idempotent.

Relevant source:

- `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Account/Services/AccountService.cs`

### Outbox And SAGA

The deposit flow persists workflow state and outbox records, then dispatches and processes them asynchronously. Partial failures move into compensation or `PendingReview`.

Relevant source:

- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

### Full-Stack Workflow Coverage

The project includes both internal operations UX and customer-facing UX, each aligned to its own role and trust boundary.

Relevant source:

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`
- `src/Banking.CustomerPortal/src/App.tsx`

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
- `WebApplicationFactory`
- `OpenAPI / Swagger`

## Quick Start

### Prerequisites

- .NET SDK 10
- Node.js
- Docker Desktop

### Run All Tests

```powershell
dotnet test BasicBankingSystem.slnx
```

### Run Frontends Locally

```powershell
cd src/Banking.Web
npm install
npm run dev
```

```powershell
cd src/Banking.CustomerPortal
npm install
npm run dev
```

### Run Docker Desktop Stack

```powershell
docker compose --env-file infra/docker.env.local -f infra/docker-compose.docker-desktop.yml up --build -d
```

## Documentation Roadmap

### Architecture And Design

- [Showcase Overview](docs/19-showcase-overview.md)
- [Microservices And Boundaries](docs/20-microservices-and-boundaries.md)
- [Saga, Outbox, And Idempotency](docs/21-saga-outbox-idempotency.md)
- [Database Schema And Relationships](docs/29-database-schema-and-relationships.md)
- [Source Code Reading Guide](docs/30-source-code-reading-guide.md)

### Testing And Contracts

- [Testing And Quality](docs/22-testing-and-quality.md)
- [OpenAPI And API Contracts](docs/23-openapi-and-api-contracts.md)
- [End-to-End Manual Test Guide](docs/13-end-to-end-manual-test.md)
- [Postman Testing Guide](docs/14-postman-testing.md)
- [Postman Runner and Newman Guide](docs/15-postman-runner-and-newman.md)

### Portfolio And Interview Material

- [One-Page Showcase Summary](docs/24-showcase-one-page-summary.md)
- [Showcase Talk Track](docs/25-showcase-talk-track.md)
- [Resume Project Bullets](docs/26-resume-project-bullets.md)
- [GitHub About Snippets](docs/27-github-about-snippets.md)
- [Interview Q And A](docs/28-interview-q-and-a.md)

### Additional Project Docs

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
- [Local Infrastructure with Docker Compose](docs/11-local-infrastructure.md)
- [Docker Desktop Run Guide](docs/12-docker-desktop-run.md)
- [Frontend Technical Guide](docs/16-frontend-technical-guide.md)
- [Backend Technical Guide](docs/17-backend-technical-guide.md)
- [Customer Portal Overview](docs/18-customer-portal-overview.md)

 
