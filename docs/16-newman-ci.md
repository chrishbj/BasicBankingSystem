# Newman CI Guide

This repository includes both a local Newman script and a GitHub Actions workflow skeleton for automated API regression checks.

## Files

- Local script: `scripts/run-newman-local.ps1`
- Workflow: `.github/workflows/newman-local-regression.yml`

## Local Usage

Run against an already running Docker Desktop stack:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1
```

If you want the script to start the local stack first:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1 -EnsureDockerStack
```

If you want a larger polling window:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-newman-local.ps1 -PollMaxAttempts 30
```

## Workflow Intent

The GitHub Actions workflow is designed to:

1. build and start the Docker-based local stack
2. wait for service health endpoints
3. execute the Postman collection through Newman
4. fail the run if the end-to-end flow fails

## Recommended Trigger

The initial workflow uses:

- `workflow_dispatch`

This keeps the first CI version safe and controllable while the environment is still evolving.

You can later extend it to:

- `pull_request`
- `push`

when you are ready to enforce it on code changes.
