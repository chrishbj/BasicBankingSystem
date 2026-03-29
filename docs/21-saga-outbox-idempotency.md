# Saga, Outbox, And Idempotency

## Why These Patterns Matter

Financial operations must avoid duplicate posting, partial success without recovery, and “saved but never published” integration failures.

This project addresses those risks with:

- `Idempotency`
- `Outbox`
- `SAGA-style processing`
- `PendingReview` fallback and compensation

## Idempotency

Deposit creation requires an `Idempotency-Key`.

This prevents duplicate deposit effects when clients retry the same request.

Relevant source:

- `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Data/DepositDbContext.cs`
- `src/Banking.Services.Deposit/Repositories/EfDepositRepository.cs`

Key implementation detail:

- `DepositService` first checks for an existing transaction by idempotency key
- the database also enforces uniqueness on `IdempotencyKey`

## Outbox Pattern

When a deposit is created, the service stores:

- the deposit transaction
- the outbox message

in the same local persistence boundary.

Relevant source:

- `src/Banking.Services.Deposit/Data/DepositDbContext.cs`
- `src/Banking.Services.Deposit/Messaging/DepositOutboxMessage.cs`
- `src/Banking.Services.Deposit/Repositories/EfDepositRepository.cs`

Then a background worker dispatches pending outbox records.

Relevant source:

- `src/Banking.Services.Deposit/Messaging/DepositOutboxDispatcher.cs`

This reduces the classic failure mode where the database commit succeeds but the integration event is lost.

## SAGA-Style Transaction Flow

The deposit process is modeled as multiple steps:

- request accepted
- account posting started
- account posting succeeded or failed
- audit write succeeded or failed
- compensation attempted when needed
- review required if compensation cannot finish cleanly

Relevant source:

- `src/Banking.Services.Deposit/Domain/DepositTransaction.cs`
- `src/Banking.Services.Deposit/Domain/DepositSagaStepStatus.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`

The account side also supports compensating behavior.

Relevant source:

- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.Services.Account/Contracts/ReverseDepositRequest.cs`

## Pending Review And Human Recovery

If the workflow cannot fully compensate, the transaction moves to `PendingReview`.

The system supports:

- retry compensation
- mark externally reversed
- mark externally failed
- automatic retry worker for review items

Relevant source:

- `src/Banking.Services.Deposit/Controllers/DepositsController.cs`
- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositPendingReviewRetryWorker.cs`

## Why Not Distributed Transactions

Distributed transactions would create tighter coupling across services and infrastructure.

This project prefers:

- local ACID transactions per service
- messaging for cross-service flow
- explicit status and compensation handling

That approach is more realistic for cloud-oriented, service-based systems.
