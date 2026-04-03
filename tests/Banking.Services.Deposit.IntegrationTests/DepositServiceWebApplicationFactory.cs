using Banking.Testing.Shared;

namespace Banking.Services.Deposit.IntegrationTests;

public sealed class DepositServiceWebApplicationFactory : SqliteWebApplicationFactory<Program>
{
    public DepositServiceWebApplicationFactory()
        : base("basicbanking-deposit-tests")
    {
    }

    protected override IReadOnlyDictionary<string, string?> GetAdditionalConfiguration()
        => new Dictionary<string, string?>
        {
            ["Infrastructure:RabbitMq:Transport"] = "InMemory",
            ["Infrastructure:RabbitMq:PollingIntervalMilliseconds"] = "50",
            ["Deposit:PendingReview:Enabled"] = "false"
        };
}
