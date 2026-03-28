# Postman Runner and Newman Guide

Use this guide when you want the local test flow to run automatically instead of clicking requests one by one.

## Files

- Collection: `postman/BasicBankingSystem-Local.postman_collection.json`
- Environment: `postman/BasicBankingSystem-Local-Docker.postman_environment.json`

## Runner Flow

The collection now includes a dedicated request for automated polling:

- `Deposits / Wait For Deposit Completion`

This request is designed for:

- Postman Collection Runner
- Newman CLI

## Recommended Automated Sequence

Run the collection in this order:

1. `Health / Customer`
2. `Health / Account`
3. `Health / Deposit`
4. `Health / Audit`
5. `Customers / Create Customer`
6. `Customers / Activate Customer`
7. `Accounts / Open Account`
8. `Deposits / Submit Deposit`
9. `Deposits / Wait For Deposit Completion`
10. `Accounts / Get Account By Id`
11. `Audits / List Audits By Correlation`

When `Wait For Deposit Completion` runs in Runner or Newman:

- it reads `transactionId`
- it polls the deposit endpoint
- it keeps calling itself until the transaction succeeds
- it fails fast if the transaction reaches `Failed`
- it stops with a test failure if the max polling attempt count is reached

## Polling Variables

Collection variables:

- `pollAttempt`
- `pollMaxAttempts`

Environment variable:

- `apiKey`

Default value:

- `pollMaxAttempts = 20`

You can change `pollMaxAttempts` in Postman before running the collection if your local machine is slower.

## Postman Collection Runner

1. Open the collection in Postman.
2. Choose `Run collection`.
3. Select the `BasicBankingSystem Local Docker` environment.
4. Keep the request order as listed above.
5. Start the run.

Expected result:

- deposit status reaches `Succeeded`
- account balance reflects the deposit amount
- audit list contains the correlation id from the deposit request

## Newman Example

If Newman is installed, you can run:

```powershell
newman run postman/BasicBankingSystem-Local.postman_collection.json `
  -e postman/BasicBankingSystem-Local-Docker.postman_environment.json
```

## Notes

- `Get Deposit By Id` remains useful for manual debugging.
- `Wait For Deposit Completion` is the better choice for automated runs.
- The collection uses unique customer identity and mobile values on each run to avoid duplicate-customer conflicts.
- The default local external API key is `local-dev-api-key`.
