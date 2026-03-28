# Docker Desktop Run Guide

This guide starts the current backend stack on Docker Desktop.

## What It Starts

- `postgres`
- `rabbitmq`
- `customer-service`
- `account-service`
- `deposit-service`
- `audit-service`

## Prerequisites

- Docker Desktop
- Docker Compose v2

## Start The Stack

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.docker-desktop.yml up --build -d
```

## Stop The Stack

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.docker-desktop.yml down
```

## Stop And Remove Data

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.docker-desktop.yml down -v
```

## Exposed Endpoints

- `Customer`: `http://localhost:5101`
- `Account`: `http://localhost:5102`
- `Deposit`: `http://localhost:5103`
- `Audit`: `http://localhost:5104`
- `RabbitMQ Management`: `http://localhost:15672`

## Swagger UI

- `Customer Swagger`: `http://localhost:5101/swagger`
- `Account Swagger`: `http://localhost:5102/swagger`
- `Deposit Swagger`: `http://localhost:5103/swagger`
- `Audit Swagger`: `http://localhost:5104/swagger`

## Authentication Headers

Protected business endpoints now require authentication.

External testing header:

- `X-Api-Key: local-dev-api-key`

Internal service-to-service headers are attached automatically by the services:

- `X-Service-Name`
- `X-Service-Key`

## Quick Checks

```powershell
Invoke-RestMethod http://localhost:5101/api/v1/health
Invoke-RestMethod http://localhost:5102/api/v1/health
Invoke-RestMethod http://localhost:5103/api/v1/health
Invoke-RestMethod http://localhost:5104/api/v1/health
```

## Notes

- Services use PostgreSQL inside Docker instead of SQLite.
- `deposit-service` talks to `account-service` and `audit-service` through container DNS names.
- RabbitMQ is enabled for the event-driven deposit flow.
- Health and readiness endpoints remain anonymous for local diagnostics.
