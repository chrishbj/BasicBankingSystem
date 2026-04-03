# Testing Roadmap

## Purpose

This document captures the most useful next steps after the current round of unit, integration, and contract-test improvements.

It is intentionally practical. The goal is to preserve momentum and make the next testing investments easier to choose.

## Near-Term Priorities

### 1. Define Internal-Service Contracts

The highest-value remaining gap is the internal service-to-service API surface.

Recommended scope for a follow-up contract document:

- `POST /api/v1/accounts/{accountId}/deposit-postings`
- `POST /api/v1/accounts/{accountId}/deposit-reversals`
- `POST /api/v1/audits`

Why this matters:

- these endpoints are already stable and business-critical
- they are currently protected mostly by integration tests
- they should not be mixed into the public service API contract

Recommended output:

- a dedicated internal contract document such as `docs/openapi-internal-services.yaml`
- matching contract tests in `tests/Banking.Contracts.Tests` or a separate internal-contract project if the scope grows

### 2. Reconcile Contract Docs With Integration Behavior

Add a small set of high-value checks that compare documented contract expectations to observed API behavior.

Start with:

- critical status codes
- `ProblemDetails` shape
- paged response shape
- required headers on documented endpoints

The goal is not to duplicate all integration tests. The goal is to detect contract drift earlier.

### 3. Continue Hardening Schema-Level Contract Checks

The current contract tests now cover key paths, response codes, enums, and core request and response fields.

Good next schema checks:

- `AuditSummary` and `AuditDetail` property-level validation
- `DepositSummary` and pending-review schema details
- nullable versus required fields on high-value DTOs
- query parameter names for documented filter and paging endpoints

### 4. Decide Whether Gateway And BFF Need Formal Contracts

Gateway and Customer Portal BFF are already protected by integration tests, but they are not yet formal OpenAPI contract surfaces.

Make an explicit call:

- if they remain app-facing implementation boundaries, keep them integration-test only
- if they become long-term public or partner-facing APIs, promote them into dedicated contract documents

### 5. Keep Unit-Test Boundaries Clean

The recent service-level refactor corrected most fake-backed unit tests.

Going forward, review new tests for:

- accidental in-memory repository usage in unit tests
- missing interaction verification on writes
- missing forbidden-side-effect assertions on error paths
- drift away from `Support/` helpers toward copy-paste setup

## Medium-Term Improvements

### 1. Add Contract Coverage To CI Reporting

Make contract-test results easier to distinguish from unit and integration results in CI summaries.

### 2. Add Internal Test Templates

Provide a small repo template for:

- service unit tests
- service integration tests
- contract tests

This will make the current conventions easier to repeat.

### 3. Review Test Naming And Project Ownership

Keep test ownership aligned with architectural boundaries:

- unit tests belong near the production module they isolate
- contract tests protect documented public or internal API surfaces
- integration tests protect composed runtime behavior

## Decision Notes

Current default position:

- public backend service APIs are documented in `docs/openapi-phase1.yaml`
- Gateway and Customer Portal BFF stay outside current contract scope
- demo-only and internal-only endpoints should not silently drift into the public contract

Any change to those decisions should be reflected in both the OpenAPI document and the contract tests.
