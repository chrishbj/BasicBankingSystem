using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Auditing;

public interface IAuditLogWriter
{
    Task WriteAsync(
        string action,
        DepositTransaction transaction,
        Dictionary<string, object?> beforeSnapshot,
        Dictionary<string, object?> afterSnapshot,
        CancellationToken cancellationToken);
}
