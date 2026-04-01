# Docker Desktop Run Guide

This guide starts the current local stack on Docker Desktop.

## What It Starts

- `postgres`
- `rabbitmq`
- `customer-service`
- `account-service`
- `deposit-service`
- `audit-service`
- `banking-web`
- `banking-customer-portal`

## Prerequisites

- Docker Desktop
- Docker Compose v2

## Start The Stack

```powershell
docker compose --env-file infra/docker.env.local -f infra/docker-compose.docker-desktop.yml up --build -d
```

## Stop The Stack

```powershell
docker compose --env-file infra/docker.env.local -f infra/docker-compose.docker-desktop.yml down
```

## Stop And Remove Data

```powershell
docker compose --env-file infra/docker.env.local -f infra/docker-compose.docker-desktop.yml down -v
```

## Exposed Endpoints

- `Customer`: `http://localhost:18081`
- `Account`: `http://localhost:18082`
- `Deposit`: `http://localhost:18083`
- `Audit`: `http://localhost:18084`
- `Operations Console`: `http://localhost:18090`
- `Customer Portal`: `http://localhost:18091`
- `RabbitMQ Management`: `http://localhost:15672`

## Swagger UI

- `Customer Swagger`: `http://localhost:18081/swagger`
- `Account Swagger`: `http://localhost:18082/swagger`
- `Deposit Swagger`: `http://localhost:18083/swagger`
- `Audit Swagger`: `http://localhost:18084/swagger`

## Frontend

- `Operations Console`: `http://localhost:18090`
- `Customer Portal`: `http://localhost:18091`

The frontend proxies API calls to the containerized backend services through Nginx, so you can use it directly without running Vite locally.

Customer portal demo sign-in:

- enter a `Customer Number`
- enter the last 4 digits of the stored identity number
- for the most reliable local demo flow, read both values from `Operations Console -> Customer Management`
- some seeded identities still normalize values such as `WITHDRAW-DEMO-001 -> 0001`

## Authentication Headers

Protected business endpoints now require authentication.

External testing header:

- `X-Api-Key: local-dev-api-key`

Internal service-to-service headers are attached automatically by the services:

- `X-Service-Name`
- `X-Service-Key`

## Quick Checks

```powershell
Invoke-RestMethod http://localhost:18081/api/v1/health
Invoke-RestMethod http://localhost:18082/api/v1/health
Invoke-RestMethod http://localhost:18083/api/v1/health
Invoke-RestMethod http://localhost:18084/api/v1/health
```

## Notes

- `infra/docker.env.local` uses a stable high-port range because some Windows environments reserve `510x` and `530x`.
- You can still customize ports by editing `infra/docker.env.local` or using `infra/.env.example` as a template.
- Services use PostgreSQL inside Docker instead of SQLite.
- `deposit-service` talks to `account-service` and `audit-service` through container DNS names.
- RabbitMQ is enabled for the event-driven deposit flow.
- Health and readiness endpoints remain anonymous for local diagnostics.
