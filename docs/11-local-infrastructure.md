# Local Infrastructure with Docker Compose

## Purpose

This guide adds local infrastructure for the backend-only solution without requiring any cloud services.

The current business services still use in-memory persistence, but this infrastructure layer is the next step toward:

- local PostgreSQL-backed development
- local RabbitMQ-based messaging
- future Outbox and SAGA implementation

## Included Infrastructure

The compose file starts:

- `PostgreSQL`
- `RabbitMQ` with management UI

Compose file:

- [infra/docker-compose.infrastructure.yml](../infra/docker-compose.infrastructure.yml)

Environment template:

- [infra/.env.example](../infra/.env.example)

## Start Infrastructure

From the repository root:

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.infrastructure.yml up -d
```

## Stop Infrastructure

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.infrastructure.yml down
```

To also remove named volumes:

```powershell
docker compose --env-file infra/.env.example -f infra/docker-compose.infrastructure.yml down -v
```

## Local Endpoints

### PostgreSQL

- Host: `localhost`
- Port: `5432`
- Database: `basic_banking`
- Username: `banking`
- Password: `banking_dev_password`

### RabbitMQ

- AMQP: `amqp://banking:banking_dev_password@localhost:5672`
- Management UI: `http://localhost:15672`

## Current Integration Status

At this stage:

- Services run successfully without infrastructure
- Services are not yet using PostgreSQL
- Services are not yet publishing to RabbitMQ
- Compose is ready for the next iteration

## Recommended Next Migration Steps

### Step 1

Replace in-memory repositories with PostgreSQL-backed repositories for:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

### Step 2

Introduce RabbitMQ messaging for:

- `DepositRequested`
- `DepositPosted`
- `DepositRejected`
- audit event ingestion

### Step 3

Add Outbox processing and background workers.

## Suggested Connection Strings

### PostgreSQL

```text
Host=localhost;Port=5432;Database=basic_banking;Username=banking;Password=banking_dev_password
```

### RabbitMQ

```text
amqp://banking:banking_dev_password@localhost:5672
```

## Verification Commands

Check running containers:

```powershell
docker ps
```

Check PostgreSQL logs:

```powershell
docker logs basicbanking-postgres
```

Check RabbitMQ logs:

```powershell
docker logs basicbanking-rabbitmq
```

## Recommended Follow-Up

Once this infrastructure is up, the most useful next step is:

- wire `Deposit Service` to RabbitMQ
- introduce PostgreSQL persistence for at least one service
- start implementing Outbox and SAGA incrementally
