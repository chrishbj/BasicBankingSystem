namespace Banking.Services.Account.Domain;

public sealed class AccountPosting
{
    public string PostingReference { get; init; } = default!;
    public string AccountId { get; init; } = default!;
    public AccountPostingType PostingType { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string? CorrelationId { get; init; }
    public string? ReversalOfPostingReference { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
