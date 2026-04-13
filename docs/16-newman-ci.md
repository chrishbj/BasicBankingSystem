# Newman CI Guide

This repository includes a local Newman regression script and a GitHub Actions workflow for Docker-based API regression checks.

## Files

- Local script: `scripts/run-newman-local.ps1`
- Workflow: `.github/workflows/newman-local-regression.yml`
- Collection: `postman/BasicBankingSystem-Local.postman_collection.json`
- Environment: `postman/BasicBankingSystem-Local-Docker.postman_environment.json`

## What The Regression Flow Covers

The Newman flow is intended to validate the local Docker stack end to end through the API surface, using:

- `X-Api-Key: local-dev-api-key`
- polling for asynchronous deposit progression
- the repository Postman collection as the executable regression contract

## Local Usage

Run against an already running Docker Desktop stack:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1
```

Start the Docker stack first and then run Newman:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1 -EnsureDockerStack
```

Use a larger polling window for slower environments:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1 -PollMaxAttempts 30
```

## Current Script Behavior

The local script:

1. optionally starts the Docker Desktop stack
2. sets `NEWMAN_POLL_MAX_ATTEMPTS`
3. runs the Postman collection through `npx newman`
4. injects the API key and polling settings as environment variables

Relevant source:

- `scripts/run-newman-local.ps1`

## Workflow Intent

The GitHub Actions workflow is designed to:

1. build and start the Docker-based stack
2. wait for the local environment to become ready
3. run the Postman collection through Newman
4. fail the workflow if the regression flow fails

## Why This Matters

This repository uses unit tests, integration tests, and contract tests, but Newman still adds value because it exercises:

- the Docker-hosted service composition
- API-key-based access paths
- real request sequencing across services
- asynchronous deposit polling from a client point of view

That makes it a useful outer-layer regression check, especially for showcase demos and local environment validation.
