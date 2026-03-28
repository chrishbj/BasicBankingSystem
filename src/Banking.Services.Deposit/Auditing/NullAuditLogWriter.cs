using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Auditing;

public sealed class NullAuditLogWriter : IAuditLogWriter
{
    public Task WriteAsync(
        string action,
        DepositTransaction transaction,
        Dictionary<string, object?> beforeSnapshot,
        Dictionary<string, object?> afterSnapshot,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
