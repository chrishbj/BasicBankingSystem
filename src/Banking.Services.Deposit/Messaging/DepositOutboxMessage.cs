using System.Text.Json;

namespace Banking.Services.Deposit.Messaging;

public sealed class DepositOutboxMessage
{
    public string MessageId { get; init; } = default!;
    public string TransactionId { get; init; } = default!;
    public string MessageType { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }

    public static DepositOutboxMessage CreateRequestedMessage(DepositRequestedMessage message, DateTimeOffset occurredAt)
    {
        return new DepositOutboxMessage
        {
            MessageId = $"out_{Guid.NewGuid():N}",
            TransactionId = message.TransactionId,
            MessageType = nameof(DepositRequestedMessage),
            Payload = JsonSerializer.Serialize(message),
            OccurredAt = occurredAt
        };
    }
}
