# BasicBankingSystem

A portfolio-ready banking platform prototype that demonstrates microservices, idempotent financial APIs, SAGA-style transaction handling, OpenAPI-driven integration, and full-stack delivery with separate operator and customer experiences.

## Why This Project Matters

This repository was built as a technical showcase rather than a simple CRUD demo.

It demonstrates:

- domain-oriented microservice boundaries
- resilient deposit processing with `Idempotency-Key`, `Outbox`, and `SAGA`
- explicit compensation and `PendingReview` recovery
- separate React applications for bank staff, customers, and platform operators
- layered testing with unit, integration, and contract coverage
- OpenAPI and Swagger as first-class integration surfaces

## Live Local Surfaces

When the Docker Desktop stack is running, the main entry points are:

- `Platform Operations Console`: `http://localhost:18089`
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

The safest way to get current demo credentials is:

- open `Operations Console`
- browse existing customers
- read `Customer Number`
- read `Portal Sign-In Last 4 Digits`

Some seeded demo identities still normalize values such as `WITHDRAW-DEMO-001 -> 0001`, but newer runtime customers may have different last-4 values.

## Core System Shape

### Backend Services

- `Customer Service`: customer master data and portal sign-in validation
- `Account Service`: account lifecycle, balance ownership, and account activity
- `Deposit Service`: idempotent deposit intake, outbox dispatch, async processing, and compensation
- `Audit Service`: audit persistence and retrieval

### Frontends

- `Banking.Web`: operator-facing operations console
- `Banking.CustomerPortal`: customer-facing self-service portal
- `Banking.PlatformOps`: platform operations console shell for runtime monitoring and diagnostics

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

The platform now separates request protection into two layers:

- all HTTP endpoints receive shared request-rate protection
- idempotent write endpoints receive replay-aware protection

For idempotent writes such as `POST /api/v1/deposits`:

- the first retry with the same `Idempotency-Key` replays the original logical result quickly
- repeated short-window replays of the same write are progressively slowed down
- replay responses may include `X-Idempotent-Replay`, `X-Idempotency-Replay-Attempt`, and `Retry-After`

This means safe client retries stay safe, while duplicate request storms are softened without changing the business result.

Relevant source:

- `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.BuildingBlocks/Extensions/RequestProtectionExtensions.cs`
- `src/Banking.BuildingBlocks/Resilience/IdempotencyReplayProtectionMiddleware.cs`

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
- `src/Banking.PlatformOps/src/App.tsx`

### Platform Control Plane

The gateway now exposes a platform-oriented API for service monitoring, workflow summary, correlation diagnostics, deposit runtime worker status, compatibility checks, rollout summaries, and environment snapshots. The repository also includes a dedicated `Banking.PlatformOps` frontend to consume that control-plane surface.

Current implemented modules:

- `Overview`
- `Services`
- `Compatibility`
- `Rollouts`
- `Environments`
- `Workflows`
- `Diagnostics`
- `Maintenance`
- `Audit`

Current design direction:

- keep `Banking Operations Console` and `Platform Operations Console` as separate control planes
- reuse testing, diagnostics, and contract assets as governed platform capabilities
- keep the platform console as `summary + drill-through`, not a replacement observability stack
- add richer baseline history, multi-environment comparison, and support access governance next

Relevant source:

- `src/Banking.Gateway/Controllers/PlatformController.cs`
- `src/Banking.Gateway/Services/PlatformMonitoringService.cs`
- `src/Banking.PlatformOps/src/App.tsx`
- `src/Banking.PlatformOps/src/api.ts`

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

```powershell
cd src/Banking.PlatformOps
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
- [Gateway And Customer BFF Design](docs/32-gateway-and-customer-bff-design.md)
- [Platform Identity and Operations Architecture](docs/38-platform-identity-and-operations-architecture.md)
- [Platform Operations Console Detailed Design](docs/39-platform-operations-console-detailed-design.md)
- [Platform Operations Console Implementation Status](docs/41-platform-operations-console-implementation-status.md)
- [Source Code Reading Guide](docs/30-source-code-reading-guide.md)

### Testing And Contracts

- [Testing And Quality](docs/22-testing-and-quality.md)
- [OpenAPI And API Contracts](docs/23-openapi-and-api-contracts.md)
- [Request Protection and Idempotency Strategy](docs/40-request-protection-and-idempotency-strategy.md)
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

 
