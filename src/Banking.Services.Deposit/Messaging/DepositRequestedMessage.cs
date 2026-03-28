using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Messaging;

public sealed record DepositRequestedMessage(
    string TransactionId,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    DepositChannel Channel,
    string CorrelationId);
