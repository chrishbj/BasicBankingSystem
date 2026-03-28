# Postman Testing Guide

This repository includes a ready-to-import Postman collection and a local Docker environment file.

## Files

- Collection: `postman/BasicBankingSystem-Local.postman_collection.json`
- Environment: `postman/BasicBankingSystem-Local-Docker.postman_environment.json`

## Import Steps

1. Open Postman.
2. Import the collection file.
3. Import the environment file.
4. Select the `BasicBankingSystem Local Docker` environment.

## Included Request Flow

The collection contains a full Phase 1 local test flow:

1. Customer health check
2. Account health check
3. Deposit health check
4. Audit health check
5. Create customer
6. Activate customer
7. Open account
8. Submit deposit
9. Get deposit by transaction id
10. Get account by account id
11. List audits by correlation id

For automated execution, also use:

- `Deposits / Wait For Deposit Completion`

## Variables Used

Environment variables:

- `customerBaseUrl`
- `accountBaseUrl`
- `depositBaseUrl`
- `auditBaseUrl`
- `apiKey`
- `currency`
- `depositAmount`

Collection variables populated during execution:

- `customerId`
- `accountId`
- `transactionId`
- `correlationId`
- `idempotencyKey`
- `identityNumber`
- `mobile`
- `pollAttempt`
- `pollMaxAttempts`

## Recommended Order

Run requests in this order:

1. `Health / Customer`
2. `Health / Account`
3. `Health / Deposit`
4. `Health / Audit`
5. `Customers / Create Customer`
6. `Customers / Activate Customer`
7. `Accounts / Open Account`
8. `Deposits / Submit Deposit`
9. `Deposits / Get Deposit By Id`
10. `Accounts / Get Account By Id`
11. `Audits / List Audits By Correlation`

## Notes

- `Submit Deposit` sends both `Idempotency-Key` and `X-Correlation-Id`.
- External requests in the collection send `X-Api-Key`.
- `Create Customer` uses a pre-request script to generate unique values for local retesting.
- `Get Deposit By Id` can be re-run until the transaction reaches `Succeeded`.
- `Wait For Deposit Completion` is intended for Postman Runner and Newman.
- `List Audits By Correlation` filters by the correlation id generated for the deposit request.

## If You Prefer Swagger

Swagger UI remains available at:

- `http://localhost:5101/swagger`
- `http://localhost:5102/swagger`
- `http://localhost:5103/swagger`
- `http://localhost:5104/swagger`
