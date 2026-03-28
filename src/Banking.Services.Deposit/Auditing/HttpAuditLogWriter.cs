using System.Net.Http.Json;
using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Auditing;

public sealed class HttpAuditLogWriter(HttpClient httpClient) : IAuditLogWriter
{
    public async Task WriteAsync(
        string action,
        DepositTransaction transaction,
        Dictionary<string, object?> beforeSnapshot,
        Dictionary<string, object?> afterSnapshot,
        CancellationToken cancellationToken)
    {
        var request = new CreateAuditLogRequest(
            "System",
            "deposit-service",
            action,
            "DepositTransaction",
            transaction.TransactionId,
            beforeSnapshot,
            afterSnapshot,
            transaction.CorrelationId,
            transaction.TransactionId);

        using var response = await httpClient.PostAsJsonAsync("/api/v1/audits", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
