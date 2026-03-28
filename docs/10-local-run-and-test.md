# Local Run, Test, and Publish Guide

## Purpose

This guide describes how to run, test, and publish the current backend-only solution locally without any cloud dependency.

## Current Local Topology

- `Banking.Gateway`: `http://localhost:5000`
- `Banking.Services.Customer`: `http://localhost:5101`
- `Banking.Services.Account`: `http://localhost:5102`
- `Banking.Services.Deposit`: `http://localhost:5103`
- `Banking.Services.Audit`: `http://localhost:5104`

## Prerequisites

- .NET SDK 10
- PowerShell

## Restore and Test

Run all tests:

```powershell
dotnet test BasicBankingSystem.slnx
```

## Run Services Locally

### Gateway

```powershell
dotnet run --project src/Banking.Gateway
```

### Customer Service

```powershell
dotnet run --project src/Banking.Services.Customer
```

### Account Service

```powershell
dotnet run --project src/Banking.Services.Account
```

### Deposit Service

```powershell
dotnet run --project src/Banking.Services.Deposit
```

### Audit Service

```powershell
dotnet run --project src/Banking.Services.Audit
```

## Health Check Endpoints

- `http://localhost:5000/api/v1/health`
- `http://localhost:5101/api/v1/health`
- `http://localhost:5102/api/v1/health`
- `http://localhost:5103/api/v1/health`
- `http://localhost:5104/api/v1/health`

## Suggested Manual Verification Flow

### 1. Create Customer

```http
POST http://localhost:5101/api/v1/customers
Content-Type: application/json

{
  "fullName": "Alice Teller",
  "identityType": "NationalId",
  "identityNumber": "110101199001011234",
  "mobile": "13800000001",
  "email": "alice@example.com",
  "address": {
    "country": "CN",
    "province": "Beijing",
    "city": "Beijing",
    "line1": "No.1 Road",
    "postalCode": "100000"
  },
  "riskLevel": "Low"
}
```

### 2. Open Account

Use seeded active customer id:

```http
POST http://localhost:5102/api/v1/accounts
Content-Type: application/json

{
  "customerId": "cus_active_001",
  "accountType": "Checking",
  "currency": "CNY"
}
```

### 3. Create Deposit

Use seeded account id:

```http
POST http://localhost:5103/api/v1/deposits
Content-Type: application/json
Idempotency-Key: dep-local-001

{
  "customerId": "cus_active_001",
  "accountId": "acc_active_001",
  "amount": 500.00,
  "currency": "CNY",
  "channel": 1,
  "referenceNumber": "LOCAL-REF-001",
  "note": "Local deposit test"
}
```

### 4. Record Audit Log

```http
POST http://localhost:5104/api/v1/audits
Content-Type: application/json

{
  "actorType": "User",
  "actorId": "user_001",
  "action": "DepositSucceeded",
  "aggregateType": "DepositTransaction",
  "aggregateId": "dep_001",
  "beforeSnapshot": {
    "status": "Processing"
  },
  "afterSnapshot": {
    "status": "Succeeded"
  },
  "correlationId": "corr-local-001",
  "causationId": "cmd-local-001"
}
```

## Local Publish

```powershell
dotnet publish src/Banking.Gateway/Banking.Gateway.csproj -c Release -o artifacts/Banking.Gateway
dotnet publish src/Banking.Services.Customer/Banking.Services.Customer.csproj -c Release -o artifacts/Banking.Services.Customer
dotnet publish src/Banking.Services.Account/Banking.Services.Account.csproj -c Release -o artifacts/Banking.Services.Account
dotnet publish src/Banking.Services.Deposit/Banking.Services.Deposit.csproj -c Release -o artifacts/Banking.Services.Deposit
dotnet publish src/Banking.Services.Audit/Banking.Services.Audit.csproj -c Release -o artifacts/Banking.Services.Audit
```

## Notes

- Current implementation uses in-memory storage for fast local development
- The seeded ids `cus_active_001`, `cus_frozen_001`, `acc_active_001`, and `acc_frozen_001` are for local verification only
- The next evolution step is replacing in-memory stores with local infrastructure such as PostgreSQL and a message bus
