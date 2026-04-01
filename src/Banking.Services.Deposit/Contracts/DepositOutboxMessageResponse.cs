namespace Banking.Services.Deposit.Contracts;

public sealed record DepositOutboxMessageResponse(
    string MessageId,
    string TransactionId,
    string MessageType,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ProcessedAt,
    string? LastError);

public sealed record RequeueDepositOutboxMessageRequest(
    string? RequestedBy,
    string? Note);
