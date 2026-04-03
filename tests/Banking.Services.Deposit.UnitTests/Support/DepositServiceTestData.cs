using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;

namespace Banking.Services.Deposit.UnitTests.Support;

internal static class DepositServiceTestData
{
    public static DepositAccountRecord CreateAccountRecord(
        string accountId = "acc_active_001",
        string customerId = "cus_active_001",
        string accountNumber = "6222202604029999",
        string currency = "USD",
        DepositAccountStatus status = DepositAccountStatus.Active)
        => new()
        {
            AccountId = accountId,
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Currency = currency,
            Status = status
        };

    public static DepositTransaction CreateTransaction(
        string transactionId,
        DepositStatus status,
        string customerId = "cus_active_001",
        string accountId = "acc_active_001",
        string correlationId = "corr-default",
        string? failureCode = null,
        DateTimeOffset? requestedAt = null)
    {
        var now = requestedAt ?? DateTimeOffset.UtcNow;
        return new DepositTransaction
        {
            TransactionId = transactionId,
            TransactionNumber = $"D{now:yyyyMMddHHmmssfff}",
            CustomerId = customerId,
            AccountId = accountId,
            Amount = 100m,
            Currency = "USD",
            Channel = DepositChannel.Counter,
            Status = status,
            AccountPostingStatus = DepositSagaStepStatus.Succeeded,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = status == DepositStatus.PendingReview ? DepositSagaStepStatus.Failed : DepositSagaStepStatus.Skipped,
            ReviewResolution = DepositReviewResolution.None,
            IdempotencyKey = $"idem-{transactionId}",
            CorrelationId = correlationId,
            FailureCode = failureCode,
            RequestedAt = now
        };
    }

    public static DepositOutboxMessage CreateOutboxMessage(DepositRequestedMessage message)
        => new()
        {
            MessageId = "out_001",
            TransactionId = message.TransactionId,
            MessageType = nameof(DepositRequestedMessage),
            Payload = System.Text.Json.JsonSerializer.Serialize(message),
            OccurredAt = DateTimeOffset.UtcNow
        };
}
