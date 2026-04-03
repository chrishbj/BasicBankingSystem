# Test Design Standards

## Purpose

This document defines the default testing standards for new tests in this repository.

Use it as the design baseline for:

- choosing the test layer
- choosing the test framework and helpers
- structuring test files
- deciding what should be mocked and what should be real
- keeping test suites readable, stable, and fast

## Default Stack

### Backend Platform

- `.NET 10`
- `ASP.NET Core`

### Core Test Frameworks

- `xUnit` for test execution
- `FluentAssertions` for assertions
- `Moq` for unit-test doubles and interaction verification

### Integration-Test Frameworks

- `WebApplicationFactory` for HTTP-level integration tests
- shared SQLite-backed test hosts for service integration tests
- service-specific stubs and drivers only where the boundary requires them, such as Gateway and Customer Portal BFF tests

### Regression And Contract Tools

- OpenAPI contract checks in `tests/Banking.Contracts.Tests`
- Postman collections for manual and scripted regression
- Newman for collection execution

## Test Pyramid

Use this repository-wide target balance unless there is a strong reason not to:

- around `70%` unit tests
- around `20%` integration tests
- around `10%` contract, smoke, and end-to-end tests

The goal is to keep business-rule feedback fast while still protecting HTTP behavior, persistence wiring, and external contracts.

## Test Layers

### 1. Unit Tests

Unit tests verify one class or one narrowly scoped workflow in isolation.

Use unit tests for:

- service decision logic
- domain transitions
- validation rules
- idempotency rules
- saga step transitions
- side effects on dependencies

Unit tests must:

- isolate external dependencies with `Moq`
- use real domain objects when state transitions are part of the behavior under test
- verify both returned results and important side effects
- avoid real HTTP, real database, and in-memory repository implementations as the primary dependency under test

Unit tests should usually mock:

- repositories
- service clients
- directories and lookups
- publishers
- audit writers
- background-process collaborators

Unit tests should usually keep real:

- request DTOs
- domain entities
- enums
- mapping results returned by the service under test

Default mock style:

- prefer `MockBehavior.Strict`
- explicitly set up only the calls needed by the scenario
- verify critical write operations such as `AddAsync`, `UpdateAsync`, `SavePostingAsync`, and `PublishAsync`
- verify non-occurrence for forbidden side effects when relevant

### 2. Integration Tests

Integration tests verify the composed application behavior through the real HTTP surface.

Use integration tests for:

- routing
- model binding
- filters and middleware
- status codes
- serialization
- DI wiring
- persistence behavior
- API regression protection
- proxy and BFF boundary behavior

Integration tests should:

- call endpoints through `HttpClient`
- use `WebApplicationFactory`
- keep assertions focused on contract and behavior visible at the API boundary
- assert status code first, then the response body
- verify `ProblemDetails`, pagination metadata, filtering, and sorting whenever those are part of the contract
- avoid re-testing every branch already covered by unit tests

Service integration tests should normally:

- inherit from the shared SQLite `WebApplicationFactory` base
- keep request builders and drivers in a local `Support/` folder
- seed data through the HTTP surface unless a carefully chosen internal setup step materially reduces noise

### 3. Contract Tests

Contract tests verify that public API definitions stay aligned with the intended service surface.

Use contract tests for:

- one chosen contract source, not multiple competing definitions
- OpenAPI path and method coverage
- required endpoint presence
- high-value schema stability checks
- response code expectations for public endpoints
- shared error and pagination models such as `ProblemDetails` and paged responses

Contract tests should normally:

- treat the checked OpenAPI document as the single contract source for the covered APIs
- parse and assert the document structurally instead of using broad string matching
- fail when the document contains endpoints that no longer exist in the implementation
- fail when required public endpoints are missing from the document
- stay focused on stable public contracts, not internal implementation details

In this repository, contract tests currently default to the backend service APIs documented in `docs/openapi-phase1.yaml`.

Gateway and BFF endpoints should only be added to contract scope if they are intentionally being maintained as public or semi-public contracts. Otherwise, keep them protected by integration tests.

### 4. Smoke And End-to-End Tests

Use these sparingly for:

- Docker Desktop validation
- Newman collection runs
- cross-service happy paths
- operator demo flows

These tests are slower and more environment-sensitive, so they should not replace unit or integration coverage.

## Design Rules By Test Type

### Unit-Test Rules

- Test one behavior per test.
- Name tests as `Method_Should_ExpectedBehavior_When_Context`.
- Prefer Arrange/Act/Assert structure.
- Assert business outcomes first, then dependency interactions.
- Do not use in-memory repositories as a substitute for mocking a repository interface in service unit tests.
- Do not verify framework internals.
- Keep each test independent and deterministic.

Examples:

- `CreateCustomer_Should_Fail_When_IdentityExists`
- `ApplyDeposit_Should_ReturnCurrentState_When_PostingReferenceIsIdempotent`
- `RetryCompensationAsync_Should_MoveToPendingReview_When_ReversalFails`

### Integration-Test Rules

- Test one HTTP behavior or contract concern per test.
- Assert status code first, then payload.
- Treat the response contract as part of the behavior under test.
- Prefer realistic request DTOs over direct state seeding when the API is the target.
- Use seeded or generated unique data to avoid cross-test pollution.
- Keep asynchronous polling helpers isolated in drivers so the test method stays focused on behavior.

### Contract-Test Rules

- Keep them stable and low-noise.
- Focus on public compatibility expectations rather than implementation details.
- Prefer structural OpenAPI assertions over raw text contains checks.
- Verify paths, methods, response codes, and critical schemas before adding low-value breadth.
- Do not place unrelated unit tests in the contracts project.

## What To Mock And What Not To Mock

### Mock These By Default

- repository interfaces
- cross-service directories and clients
- message publishers
- outbox collaborators
- audit writers
- external gateways

### Usually Do Not Mock These

- DTOs
- domain entities
- value-carrying records
- service return objects

### Only Use In-Memory Fakes When

- the test is intentionally a component-style test, not a strict unit test
- the fake itself is part of the thing being validated
- the test is clearly named and documented as component or slice coverage

If an in-memory implementation is used, do not call the test a pure unit test.

## File And Folder Conventions

### Project Naming

- `tests/<Service>.UnitTests`
- `tests/<Service>.IntegrationTests`
- `tests/Banking.Contracts.Tests`

### File Naming

- one primary production class per test file when practical
- file name should mirror the production class name, for example:
  - `CustomerServiceTests.cs`
  - `DepositTransactionProcessorTests.cs`

### Shared Test Helpers

Use a local `Support/` folder inside the relevant test project for:

- domain object builders
- reusable request factories
- HTTP request drivers
- stable fixture data

Keep helper classes small and explicit. They should reduce duplication, not hide test intent.

### Shared Integration Infrastructure

When multiple service integration projects use the same test-host pattern:

- extract the shared host setup into `tests/Shared`
- link the shared source file into each integration test project
- keep only service-specific overrides in the local factory

## Coverage Priorities

When adding tests for a service, prioritize these behaviors first:

1. validation failures
2. state-transition rules
3. idempotency rules
4. failure and compensation paths
5. write-side side effects
6. read-model mapping and pagination
7. HTTP status codes and contract shape
8. error payloads and `ProblemDetails`
9. filtering, sorting, and out-of-range pagination behavior

When adding contract tests, prioritize these checks first:

1. contract source exists and is readable
2. required public paths and methods exist
3. removed or renamed endpoints are detected
4. critical response codes exist on high-value endpoints
5. shared schemas such as `ProblemDetails` and paged responses stay stable
6. critical enums stay stable

## TDD Workflow

Preferred workflow:

1. `Red`: add the smallest failing test for the next business rule
2. `Green`: implement the minimum change to pass
3. `Refactor`: remove duplication and improve names, helpers, and structure

## CI Order

Recommended pipeline order:

1. unit tests
2. contract tests
3. integration tests
4. smoke/end-to-end tests

This keeps fast feedback first and slower environment-dependent checks later.

## Current Repository Standard

As of April 2, 2026, the standard for new backend tests in this repository is:

- `xUnit`
- `FluentAssertions`
- `Moq`
- strict dependency isolation for service-level unit tests
- `WebApplicationFactory` for API integration tests
- shared SQLite-backed integration hosts for service APIs
- one chosen OpenAPI contract source for covered service APIs
- `Support/` helpers for reusable builders, drivers, and test data

New tests should follow this standard unless there is a documented reason to deviate.
