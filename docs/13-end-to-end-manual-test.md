# End-to-End Manual Test Guide

This guide walks through a full Phase 1 happy-path test against the Docker Desktop environment:

1. Create a customer
2. Activate the customer
3. Open an account
4. Submit a deposit
5. Wait for the deposit to complete
6. Verify the account balance
7. Verify audit records

## Prerequisites

- Docker Desktop is running
- The local stack is up:

```powershell
docker compose --env-file infra/docker.env.local -f infra/docker-compose.docker-desktop.yml up --build -d
```

- Swagger pages are reachable:
  - `http://localhost:18081/swagger`
  - `http://localhost:18082/swagger`
  - `http://localhost:18083/swagger`
  - `http://localhost:18084/swagger`
- Protected API calls need:
  - `X-Api-Key: local-dev-api-key`

## Fastest Option

Run the smoke test script:

```powershell
pwsh ./scripts/manual-smoke-test.ps1
```

If `pwsh` is not installed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\manual-smoke-test.ps1
```

The script will:

- verify service health
- create test data
- poll the asynchronous deposit flow
- confirm account balances
- confirm audit records

## Browser-Driven Manual Walkthrough

Use Swagger UI to execute the following calls in order.

## UI Notes

- `Operations Console` at `http://localhost:18090` shows `Customer Number` explicitly in customer cards, current selection, and portal sign-in hints.
- `Customer Portal` at `http://localhost:18091` signs in with `Customer Number + Identity Last 4 Digits`.
- For the most reliable demo sign-in, read the values from an existing customer card in the Operations Console instead of relying on a hard-coded sample.

### 1. Create a Customer

Open `http://localhost:18081/swagger` and call `POST /api/v1/customers` with:

```json
{
  "fullName": "Manual Test Customer",
  "identityType": "NationalId",
  "identityNumber": "MTC-20260328-001",
  "mobile": "13812345678",
  "email": "manual.test.customer@example.com",
  "address": {
    "country": "CN",
    "province": "Beijing",
    "city": "Beijing",
    "line1": "No. 1 Banking Road",
    "postalCode": "100000"
  },
  "riskLevel": "Low"
}
```

Capture the returned `customerId`.

### 2. Activate the Customer

Still in Customer Swagger, call `POST /api/v1/customers/{customerId}/status` with:

```json
{
  "targetStatus": 2,
  "reason": "Manual end-to-end test activation"
}
```

`2` maps to `Active`.

### 3. Open an Account

Open `http://localhost:18082/swagger` and call `POST /api/v1/accounts` with:

```json
{
  "customerId": "<customerId>",
  "accountType": "Checking",
  "currency": "USD"
}
```

Capture the returned `accountId` and `accountNumber`.

### 4. Submit a Deposit

Open `http://localhost:18083/swagger` and call `POST /api/v1/deposits`.

Add headers:

- `X-Api-Key`: `local-dev-api-key`
- `Idempotency-Key`: any unique value, for example `manual-deposit-001`
- `X-Correlation-Id`: any unique value, for example `manual-correlation-001`

Request body:

```json
{
  "customerId": "<customerId>",
  "accountId": "<accountId>",
  "amount": 1000,
  "currency": "USD",
  "channel": 1,
  "referenceNumber": "MANUAL-REF-001",
  "note": "Manual Swagger test"
}
```

`1` maps to `Counter`.

The first response should normally be `202 Accepted`. Capture the returned `transactionId`.

### 5. Check Deposit Status

In Deposit Swagger, call `GET /api/v1/deposits/{transactionId}` until:

- `status` becomes `3` (`Succeeded`), or
- `status` becomes `5` (`Failed`)

`3` means the asynchronous processing completed successfully.

### 6. Verify Account Balance

In Account Swagger, call `GET /api/v1/accounts/by-number/{accountNumber}`.

Expected result after a successful deposit:

- `availableBalance` = `1000`
- `ledgerBalance` = `1000`
- `status` remains active

### 7. Verify Audit Trail

Open `http://localhost:18084/swagger` and call `GET /api/v1/audits`.

Add header:

- `X-Api-Key`: `local-dev-api-key`

Look for a row where one of these matches:

- `correlationId` equals the `X-Correlation-Id` used in the deposit request
- `aggregateId` equals the `transactionId`

Expected action for the happy path:

- `DepositSucceeded`

## Review and Recovery Checks

The deposit service also exposes operational review endpoints in Swagger:

- `GET /api/v1/deposits/review/pending`
- `POST /api/v1/deposits/{transactionId}/review/retry-compensation`
- `POST /api/v1/deposits/{transactionId}/review/resolve`

Useful query options for operations search:

- `GET /api/v1/deposits?status=PendingReview`
- `GET /api/v1/deposits?correlationId=<value>`
- `GET /api/v1/deposits?failureCode=DEPOSIT_COMPENSATION_REVIEW_REQUIRED`
- `GET /api/v1/deposits/review/pending?sortBy=RequestedAt&descending=true`

## Expected Happy-Path Outcome

At the end of the walkthrough:

- the customer exists and is active
- the account exists with a positive balance
- the deposit transaction is `Succeeded`
- at least one audit record exists for the deposit flow

## Troubleshooting

### Swagger page does not open

Check:

```powershell
docker ps
```

Then verify:

```powershell
Invoke-WebRequest http://localhost:18083/swagger -UseBasicParsing
```

### Deposit stays in `Received`

Check:

- `basicbanking-deposit` container logs
- RabbitMQ container status
- Account service health at `http://localhost:18082/api/v1/health`
- Audit service health at `http://localhost:18084/api/v1/health`

Useful commands:

```powershell
docker logs basicbanking-deposit --tail 200
docker logs basicbanking-rabbitmq --tail 200
```

### Deposit becomes `Failed`

Common causes:

- customer and account do not match
- account is not active
- downstream account posting failed

Use:

- `GET /api/v1/deposits/{transactionId}`
- `GET /api/v1/accounts/by-number/{accountNumber}`
- `GET /api/v1/audits`

to inspect the failure context.
