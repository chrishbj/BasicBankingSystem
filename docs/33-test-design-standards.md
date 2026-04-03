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
- SQLite-backed test hosts where the service integration setup already uses SQLite

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
- directories/lookups
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
- verify critical write operations such as `AddAsync`, `UpdateAsync`, `SavePostingAsync`, `PublishAsync`
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

Integration tests should:

- call endpoints through `HttpClient`
- use `WebApplicationFactory`
- keep assertions focused on contract and behavior visible at the API boundary
- avoid re-testing every branch already covered by unit tests

### 3. Contract Tests

Contract tests verify that public API definitions stay aligned with the intended service surface.

Use contract tests for:

- OpenAPI path coverage
- required endpoint presence
- high-value schema stability checks

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
- Prefer realistic request DTOs over direct state seeding when the API is the target.
- Use seeded or generated unique data to avoid cross-test pollution.

### Contract-Test Rules

- Keep them stable and low-noise.
- Focus on public compatibility expectations rather than implementation details.

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
- the test is clearly named and documented as component/slice coverage

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
- stable fixture data

Keep helper classes small and explicit. They should reduce duplication, not hide test intent.

## Coverage Priorities

When adding tests for a service, prioritize these behaviors first:

1. validation failures
2. state-transition rules
3. idempotency rules
4. failure and compensation paths
5. write-side side effects
6. read-model mapping and pagination
7. HTTP status codes and contract shape

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

As of April 2, 2026, the standard for new backend service unit tests in this repository is:

- `xUnit`
- `FluentAssertions`
- `Moq`
- strict dependency isolation for service-level unit tests
- `WebApplicationFactory` for API integration tests

New tests should follow this standard unless there is a documented reason to deviate.
