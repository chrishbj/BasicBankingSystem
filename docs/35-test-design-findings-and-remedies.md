# Test Design Findings And Remedies

## Purpose

This document summarizes the recurring test-design problems found during the current cleanup and the practical remedies that worked in this repository.

Use it as a review checklist when adding or evaluating new tests.

## Common Problems And Solutions

### 1. Fake-Backed Unit Tests Were Testing The Wrong Thing

Problem:

- service unit tests were built around in-memory repositories
- the tests often validated fake repository behavior instead of service decisions
- failure paths and dependency interactions were under-specified

Typical symptoms:

- a test creates data in an in-memory store and then asserts a final result
- repository calls such as `AddAsync` or `UpdateAsync` are never verified
- duplicate and forbidden-side-effect paths are not isolated

Remedy:

- use `Moq` to isolate repository and collaborator boundaries
- keep real domain objects where state transition behavior matters
- verify critical writes and verify non-occurrence on error paths

### 2. Unit And Integration Boundaries Were Blurred

Problem:

- some tests had unit-test names but component-style behavior
- API tests and fake-backed service tests overlapped in responsibility

Typical symptoms:

- the same workflow is covered end-to-end in both a unit test and an integration test
- test failures do not clearly point to logic, wiring, or transport

Remedy:

- unit tests isolate one class or narrow workflow
- integration tests go through real `HttpClient` and `WebApplicationFactory`
- component-style coverage should be named and documented explicitly if kept

### 3. Integration Tests Focused Too Much On Happy Paths

Problem:

- status code checks existed, but error contracts and edge behavior were under-protected

Typical symptoms:

- tests assert only `200`, `201`, or `202`
- `ProblemDetails` payloads are not inspected
- pagination, filtering, sorting, and out-of-range pages are not asserted

Remedy:

- assert error payloads, not just status codes
- add boundary coverage for paging and filter behavior
- keep one HTTP concern per test so regressions are easier to diagnose

### 4. Integration Infrastructure Was Repeated Across Projects

Problem:

- each service integration test project repeated similar SQLite host setup

Typical symptoms:

- four slightly different `WebApplicationFactory` implementations
- repeated temporary database setup and teardown logic

Remedy:

- extract shared host setup into `tests/Shared`
- keep only service-specific overrides in each local factory
- use local `Support/` helpers for data builders and async drivers

### 5. Contract Tests Were Too Weak To Catch Real Drift

Problem:

- contract tests originally used broad string matching on the OpenAPI file
- outdated and missing endpoints could slip through without failures

Typical symptoms:

- a path disappears or changes, but tests still pass
- old endpoints remain documented long after implementation moved on
- response codes and schemas are not checked structurally

Remedy:

- use one chosen contract source
- parse the contract structurally
- assert paths, methods, response codes, schemas, enums, and key field requirements

### 6. Contract Scope Was Not Explicit Enough

Problem:

- public APIs, internal APIs, Gateway routes, BFF routes, and demo endpoints were at risk of being mixed together

Typical symptoms:

- uncertainty about whether an endpoint belongs in `openapi-phase1.yaml`
- pressure to add every stable route into one contract file

Remedy:

- define contract scope explicitly in docs
- keep public service APIs, internal service APIs, and app-facing BFF or Gateway APIs separate unless there is a strong reason to combine them

### 7. Non-Contract Unit Tests Lived In The Contracts Project

Problem:

- `BankingSecurityHeaderValidatorTests` lived under `Banking.Contracts.Tests`, even though it was a building-block unit test

Typical symptom:

- the contracts project contains tests that do not validate an API contract document

Remedy:

- move infrastructure logic tests into a matching unit-test project
- keep contract tests focused on documented surface compatibility

### 8. Supporting Docs Drifted Away From The Implemented Test Strategy

Problem:

- test guidance documents still referenced old tools, old endpoints, and outdated boundaries

Typical symptoms:

- docs mention bearer tokens when the contract now uses `X-Api-Key`
- docs list endpoints that no longer exist
- docs do not reflect current use of `Moq`, shared SQLite hosts, or structural contract checks

Remedy:

- update design docs whenever test architecture changes
- keep summary docs linked to the current source-of-truth documents
- treat documentation maintenance as part of the testing change, not a follow-up chore

## Review Checklist

When reviewing a new test or test suite, check these first:

- is the test in the right layer
- is the boundary under test isolated correctly
- does the test assert the actual contract or behavior that matters
- are error and edge cases protected
- does the test reduce ambiguity when it fails
- is shared setup extracted without hiding intent
- do the docs still match the implemented testing approach
