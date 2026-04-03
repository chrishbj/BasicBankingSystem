# Platform Diagnostics API Draft

## Purpose

This document proposes a first draft of the API surface and DTO model for the future platform diagnostics capability.

It is intentionally a design draft, not an implementation commitment. The goal is to define a clear next-step API shape for the future Platform Management Portal and privileged operator workflows.

## Relationship To Existing Health Endpoints

This draft does not replace:

- `GET /api/v1/health`
- `GET /api/v1/ready`

Those remain the simple runtime probe endpoints.

This draft defines a separate operator-facing diagnostics surface for:

- deeper inspection
- runtime contract visibility
- advanced checks
- privileged diagnostic execution

## Design Principles

- keep basic health endpoints minimal and stable
- keep diagnostics read-oriented by default
- separate passive diagnostics from control-style actions
- make expensive checks explicit
- support strong authorization and audit logging
- support future aggregation inside a Platform Management Portal

## Proposed Namespace

Recommended namespace:

- `/api/v1/system/*`

Alternative if the team wants stronger separation later:

- `/api/v1/platform/*`

For this draft, `system` is used consistently to stay close to the current Gateway direction.

## Proposed Roles

### Probe

- may access only basic health endpoints

### Operator

- may view advanced diagnostic information
- may trigger low-risk diagnostic runs

### PlatformAdmin

- may trigger privileged or expensive diagnostic runs
- may access sensitive operator views

## Endpoint Draft

### 1. System Info

`GET /api/v1/system/info`

Purpose:

- identify the platform entry service
- expose version and routing metadata

Typical response:

```json
{
  "service": "Banking.Gateway",
  "version": "v1",
  "mode": "platform-entry",
  "environment": "Production",
  "routes": {
    "customer": "/customer-api",
    "account": "/account-api",
    "deposit": "/deposit-api",
    "audit": "/audit-api"
  },
  "generatedAt": "2026-04-03T14:30:00Z"
}
```

Recommended access:

- `AllowAnonymous` or `Probe`

### 2. Health Summary

`GET /api/v1/system/health-summary`

Purpose:

- show current aggregated service health for operators

Typical response:

```json
{
  "gateway": "Banking.Gateway",
  "checkedAt": "2026-04-03T14:35:00Z",
  "overallStatus": "Healthy",
  "services": [
    {
      "name": "customer-service",
      "health": "Healthy",
      "basePath": "/customer-api",
      "swaggerUrl": "/customer-api/swagger/index.html",
      "openApiUrl": "/customer-api/openapi/v1.json"
    }
  ]
}
```

Recommended access:

- `Operator`

### 3. Diagnostics Overview

`GET /api/v1/system/diagnostics`

Purpose:

- provide a structured operator view of current diagnostic state across services and dependencies

Suggested query parameters:

- `scope=all|service|dependency|workflow|contract`
- `target=customer-service|account-service|deposit-service|audit-service`

Typical response:

```json
{
  "checkedAt": "2026-04-03T14:40:00Z",
  "overallStatus": "Degraded",
  "summary": {
    "healthy": 7,
    "degraded": 1,
    "failed": 0
  },
  "checks": [
    {
      "checkId": "deposit-openapi",
      "category": "Contract",
      "target": "deposit-service",
      "status": "Degraded",
      "severity": "Warning",
      "title": "Runtime OpenAPI mismatch",
      "detail": "One documented enum value differs from the current baseline.",
      "startedAt": "2026-04-03T14:39:59Z",
      "completedAt": "2026-04-03T14:40:00Z"
    }
  ]
}
```

Recommended access:

- `Operator`

### 4. Single Diagnostic Check

`GET /api/v1/system/diagnostics/{checkId}`

Purpose:

- inspect one diagnostic result in more detail

Typical response:

```json
{
  "checkId": "deposit-openapi",
  "category": "Contract",
  "target": "deposit-service",
  "status": "Degraded",
  "severity": "Warning",
  "title": "Runtime OpenAPI mismatch",
  "detail": "One documented enum value differs from the current baseline.",
  "recommendation": "Review the deployed OpenAPI document and compare it to the approved baseline.",
  "evidence": {
    "runtimeOpenApiUrl": "/deposit-api/openapi/v1.json",
    "baselineVersion": "phase1-2026-04-03"
  },
  "startedAt": "2026-04-03T14:39:59Z",
  "completedAt": "2026-04-03T14:40:00Z"
}
```

Recommended access:

- `Operator`

### 5. Trigger Diagnostic Run

`POST /api/v1/system/diagnostics/run`

Purpose:

- run a selected diagnostic suite on demand

Suggested request:

```json
{
  "scope": "Contract",
  "targets": ["deposit-service"],
  "checks": ["openapi-availability", "baseline-drift"],
  "mode": "ReadOnly",
  "requestedBy": "ops.alice"
}
```

Suggested synchronous response for quick runs:

```json
{
  "runId": "diagrun_202604031445001",
  "status": "Accepted",
  "submittedAt": "2026-04-03T14:45:00Z",
  "mode": "Async"
}
```

Recommended access:

- `Operator` for safe read-only runs
- `PlatformAdmin` for heavier or privileged runs

### 6. Diagnostic Run Status

`GET /api/v1/system/diagnostics/runs/{runId}`

Purpose:

- inspect progress and results of an asynchronous diagnostic run

Typical response:

```json
{
  "runId": "diagrun_202604031445001",
  "status": "Completed",
  "scope": "Contract",
  "submittedAt": "2026-04-03T14:45:00Z",
  "startedAt": "2026-04-03T14:45:02Z",
  "completedAt": "2026-04-03T14:45:05Z",
  "results": [
    {
      "checkId": "deposit-openapi",
      "status": "Healthy"
    }
  ]
}
```

Recommended access:

- `Operator`

### 7. Runtime Contracts Overview

`GET /api/v1/system/contracts`

Purpose:

- show current runtime contract status for each documented service

Typical response:

```json
{
  "checkedAt": "2026-04-03T14:50:00Z",
  "services": [
    {
      "service": "deposit-service",
      "openApiUrl": "/deposit-api/openapi/v1.json",
      "status": "Healthy",
      "parseable": true,
      "baseline": "phase1-2026-04-03",
      "lastVerifiedAt": "2026-04-03T14:49:58Z"
    }
  ]
}
```

Recommended access:

- `Operator`

### 8. Verify Runtime Contract

`POST /api/v1/system/contracts/verify`

Purpose:

- compare runtime OpenAPI to an approved baseline or policy set

Suggested request:

```json
{
  "targets": ["customer-service", "deposit-service"],
  "baseline": "phase1-2026-04-03",
  "checks": ["paths", "responses", "schemas", "enums"]
}
```

Suggested response:

```json
{
  "runId": "contractrun_202604031500001",
  "status": "Accepted",
  "submittedAt": "2026-04-03T15:00:00Z"
}
```

Recommended access:

- `Operator` or `PlatformAdmin`, depending on runtime cost

## Suggested DTO Model

### Diagnostic Check Summary

```json
{
  "checkId": "deposit-openapi",
  "category": "Contract",
  "target": "deposit-service",
  "status": "Healthy",
  "severity": "Info",
  "title": "Runtime OpenAPI available",
  "detail": "OpenAPI endpoint responded and parsed successfully.",
  "startedAt": "2026-04-03T14:39:59Z",
  "completedAt": "2026-04-03T14:40:00Z"
}
```

### Diagnostic Check Detail

```json
{
  "checkId": "deposit-openapi",
  "category": "Contract",
  "target": "deposit-service",
  "status": "Healthy",
  "severity": "Info",
  "title": "Runtime OpenAPI available",
  "detail": "OpenAPI endpoint responded and parsed successfully.",
  "recommendation": null,
  "evidence": {
    "openApiUrl": "/deposit-api/openapi/v1.json"
  },
  "startedAt": "2026-04-03T14:39:59Z",
  "completedAt": "2026-04-03T14:40:00Z"
}
```

### Diagnostic Run Request

```json
{
  "scope": "Dependency",
  "targets": ["deposit-service"],
  "checks": ["database-connectivity", "audit-service-auth"],
  "mode": "ReadOnly",
  "requestedBy": "ops.alice"
}
```

### Diagnostic Run Result

```json
{
  "runId": "diagrun_202604031445001",
  "status": "Completed",
  "scope": "Dependency",
  "submittedAt": "2026-04-03T14:45:00Z",
  "startedAt": "2026-04-03T14:45:02Z",
  "completedAt": "2026-04-03T14:45:05Z",
  "results": []
}
```

## Suggested Enums

### DiagnosticStatus

- `Healthy`
- `Degraded`
- `Failed`
- `Skipped`

### DiagnosticSeverity

- `Info`
- `Warning`
- `Critical`

### DiagnosticCategory

- `Service`
- `Dependency`
- `Workflow`
- `Contract`

### DiagnosticRunStatus

- `Accepted`
- `Running`
- `Completed`
- `Failed`
- `Cancelled`

## Audit And Security Requirements

The API draft assumes:

- all advanced diagnostic runs are authenticated
- all diagnostic run requests are audited
- sensitive evidence is redacted before being returned
- heavy checks may be asynchronous

Audit fields to capture:

- requester identity
- request time
- scope
- targets
- result status

## Out Of Scope For This Draft

- final persistence model for diagnostic history
- actual runtime implementation
- UI design for the Platform Management Portal
- final authorization provider integration

## Recommended Next Step

If implementation starts in the next phase, the most practical order is:

1. finalize route and DTO names
2. expose runtime OpenAPI in contract-testing environments
3. add runtime contract verification endpoints
4. add dependency and workflow diagnostics incrementally
