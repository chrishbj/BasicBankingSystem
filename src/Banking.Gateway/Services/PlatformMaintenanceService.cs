using System.Net.Http.Json;
using System.Text.Json;
using Banking.Gateway.Contracts;

namespace Banking.Gateway.Services;

public sealed class PlatformMaintenanceService(IHttpClientFactory httpClientFactory)
{
    public async Task<PlatformMaintenanceActionResponse> RetryDepositCompensationAsync(
        string transactionId,
        string actorId,
        string reason,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var depositClient = httpClientFactory.CreateClient("deposit-service");

        using var depositResponse = await depositClient.PostAsJsonAsync(
            $"api/v1/deposits/{Uri.EscapeDataString(transactionId)}/review/retry-compensation",
            new
            {
                operatorId = actorId,
                note = reason
            },
            cancellationToken);

        var resultStatus = depositResponse.IsSuccessStatusCode ? "Succeeded" : "Failed";
        var operationId = await RecordMaintenanceAuditAsync(
            actorId,
            transactionId,
            reason,
            correlationId,
            resultStatus,
            (int)depositResponse.StatusCode,
            occurredAt,
            "PlatformMaintenanceRetryCompensation",
            null,
            cancellationToken);

        return new PlatformMaintenanceActionResponse(
            operationId,
            "RetryDepositCompensation",
            "DepositTransaction",
            transactionId,
            actorId,
            resultStatus,
            (int)depositResponse.StatusCode,
            reason,
            occurredAt);
    }

    public async Task<PlatformMaintenanceActionResponse> ResolveDepositReviewAsync(
        string transactionId,
        string actorId,
        string resolution,
        string reason,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var depositClient = httpClientFactory.CreateClient("deposit-service");

        using var depositResponse = await depositClient.PostAsJsonAsync(
            $"api/v1/deposits/{Uri.EscapeDataString(transactionId)}/review/resolve",
            new
            {
                resolution = MapResolution(resolution),
                operatorId = actorId,
                note = reason
            },
            cancellationToken);

        var resultStatus = depositResponse.IsSuccessStatusCode ? "Succeeded" : "Failed";
        var operationId = await RecordMaintenanceAuditAsync(
            actorId,
            transactionId,
            reason,
            correlationId,
            resultStatus,
            (int)depositResponse.StatusCode,
            occurredAt,
            "PlatformMaintenanceResolveDepositReview",
            new Dictionary<string, object?>
            {
                ["resolution"] = resolution
            },
            cancellationToken);

        return new PlatformMaintenanceActionResponse(
            operationId,
            "ResolveDepositReview",
            "DepositTransaction",
            transactionId,
            actorId,
            resultStatus,
            (int)depositResponse.StatusCode,
            reason,
            occurredAt);
    }

    public async Task<PlatformMaintenanceActionResponse> RequeueOutboxMessageAsync(
        string messageId,
        string actorId,
        string reason,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var depositClient = httpClientFactory.CreateClient("deposit-service");

        using var depositResponse = await depositClient.PostAsJsonAsync(
            $"api/v1/deposits/outbox/{Uri.EscapeDataString(messageId)}/requeue",
            new
            {
                requestedBy = actorId,
                note = reason
            },
            cancellationToken);

        var resultStatus = depositResponse.IsSuccessStatusCode ? "Succeeded" : "Failed";
        var operationId = await RecordMaintenanceAuditAsync(
            actorId,
            messageId,
            reason,
            correlationId,
            resultStatus,
            (int)depositResponse.StatusCode,
            occurredAt,
            "PlatformMaintenanceRequeueOutboxMessage",
            null,
            cancellationToken);

        return new PlatformMaintenanceActionResponse(
            operationId,
            "RequeueOutboxMessage",
            "DepositOutboxMessage",
            messageId,
            actorId,
            resultStatus,
            (int)depositResponse.StatusCode,
            reason,
            occurredAt);
    }

    private async Task<string> RecordMaintenanceAuditAsync(
        string actorId,
        string transactionId,
        string reason,
        string? correlationId,
        string resultStatus,
        int downstreamStatusCode,
        DateTimeOffset occurredAt,
        string action,
        Dictionary<string, object?>? additionalAfterSnapshot,
        CancellationToken cancellationToken)
    {
        var auditClient = httpClientFactory.CreateClient("audit-service");
        var afterSnapshot = new Dictionary<string, object?>
        {
            ["reason"] = reason,
            ["resultStatus"] = resultStatus,
            ["downstreamStatusCode"] = downstreamStatusCode,
            ["occurredAt"] = occurredAt
        };

        if (additionalAfterSnapshot is not null)
        {
            foreach (var entry in additionalAfterSnapshot)
            {
                afterSnapshot[entry.Key] = entry.Value;
            }
        }

        using var response = await auditClient.PostAsJsonAsync(
            "api/v1/audits",
            new
            {
                actorType = "PlatformOperator",
                actorId,
                action,
                aggregateType = action.Contains("Outbox", StringComparison.Ordinal) ? "DepositOutboxMessage" : "DepositTransaction",
                aggregateId = transactionId,
                beforeSnapshot = (object?)null,
                afterSnapshot,
                correlationId,
                causationId = $"platform-maintenance-{transactionId}-{occurredAt.ToUnixTimeMilliseconds()}"
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"op_{Guid.NewGuid():N}";
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        if (document.RootElement.TryGetProperty("auditId", out var auditIdElement))
        {
            return auditIdElement.GetString() ?? $"op_{Guid.NewGuid():N}";
        }

        return $"op_{Guid.NewGuid():N}";
    }

    private static int MapResolution(string resolution)
        => resolution.Trim().ToLowerInvariant() switch
        {
            "reversedexternally" or "reversed" => 3,
            "failedexternally" or "failed" => 4,
            _ => throw new InvalidOperationException($"Resolution '{resolution}' is not supported.")
        };
}
