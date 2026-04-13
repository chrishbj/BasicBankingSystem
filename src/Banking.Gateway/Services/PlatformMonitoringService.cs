using System.Net.Http.Json;
using System.Text.Json;
using Banking.BuildingBlocks.Contracts;
using Banking.Gateway.Contracts;

namespace Banking.Gateway.Services;

public sealed class PlatformMonitoringService(
    GatewayHealthService gatewayHealthService,
    IHttpClientFactory httpClientFactory,
    IHostEnvironment hostEnvironment)
{
    public async Task<PlatformOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var healthSummary = await gatewayHealthService.GetSummaryAsync(cancellationToken);
        var depositSummary = await GetDepositWorkflowSummaryAsync(cancellationToken);
        var depositRuntime = await GetDepositRuntimeStatusAsync(cancellationToken);

        var services = healthSummary.Services
            .Select(service => new PlatformServiceStatusResponse(
                service.Name,
                service.BasePath,
                service.Health,
                service.StatusCode,
                service.SwaggerUrl,
                service.OpenApiUrl))
            .ToArray();

        var dependencies = services
            .Select(service => new PlatformDependencyStatusResponse(
                service.Name,
                service.Health,
                "gateway"))
            .ToArray();

        return new PlatformOverviewResponse(
            "BasicBankingSystem",
            DateTimeOffset.UtcNow,
            services,
            dependencies,
            depositSummary,
            depositRuntime);
    }

    public async Task<IReadOnlyCollection<PlatformServiceStatusResponse>> GetServicesAsync(CancellationToken cancellationToken)
    {
        var healthSummary = await gatewayHealthService.GetSummaryAsync(cancellationToken);
        return healthSummary.Services
            .Select(service => new PlatformServiceStatusResponse(
                service.Name,
                service.BasePath,
                service.Health,
                service.StatusCode,
                service.SwaggerUrl,
                service.OpenApiUrl))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<PlatformCompatibilityStatusResponse>> GetCompatibilityAsync(CancellationToken cancellationToken)
    {
        var services = await GetServicesAsync(cancellationToken);
        var checks = await Task.WhenAll(services.Select(service => BuildCompatibilityStatusAsync(service, cancellationToken)));
        return checks;
    }

    public async Task<IReadOnlyCollection<PlatformRolloutStatusResponse>> GetRolloutsAsync(CancellationToken cancellationToken)
    {
        var services = await GetServicesAsync(cancellationToken);
        var checkedAt = DateTimeOffset.UtcNow;

        return services
            .Select(service =>
            {
                var healthy = string.Equals(service.Health, "Healthy", StringComparison.OrdinalIgnoreCase);
                return new PlatformRolloutStatusResponse(
                    service.Name,
                    hostEnvironment.EnvironmentName,
                    "v1",
                    "v1",
                    healthy ? "Stable" : "Investigate",
                    healthy ? 100 : 0,
                    service.Health,
                    healthy ? "Compatible" : "Unknown",
                    checkedAt);
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<PlatformEnvironmentSummaryResponse>> GetEnvironmentsAsync(CancellationToken cancellationToken)
    {
        var services = await GetServicesAsync(cancellationToken);
        var healthyCount = services.Count(service => string.Equals(service.Health, "Healthy", StringComparison.OrdinalIgnoreCase));

        return
        [
            new PlatformEnvironmentSummaryResponse(
                hostEnvironment.EnvironmentName,
                "Banking.Gateway",
                DateTimeOffset.UtcNow,
                services.Count,
                healthyCount,
                "openapi-phase1",
                "platform-surface-draft",
                false,
                "Single-environment local-first snapshot. Add more environments later for cross-environment comparison.")
        ];
    }

    public async Task<DepositWorkflowSummaryResponse> GetDepositWorkflowSummaryAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("deposit-service");

        var receivedCountTask = SafeGetDepositCountAsync(client, "Received", cancellationToken);
        var succeededCountTask = SafeGetDepositCountAsync(client, "Succeeded", cancellationToken);
        var failedCountTask = SafeGetDepositCountAsync(client, "Failed", cancellationToken);
        var pendingReviewTask = SafeGetPendingReviewAsync(client, cancellationToken);

        await Task.WhenAll(receivedCountTask, succeededCountTask, failedCountTask, pendingReviewTask);

        return new DepositWorkflowSummaryResponse(
            DateTimeOffset.UtcNow,
            receivedCountTask.Result,
            succeededCountTask.Result,
            failedCountTask.Result,
            pendingReviewTask.Result.TotalCount,
            pendingReviewTask.Result.Items.ToArray());
    }

    public async Task<IReadOnlyCollection<DepositPendingReviewItemResponse>> GetPendingReviewAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("deposit-service");
        var pendingReview = await SafeGetPendingReviewAsync(client, cancellationToken);
        return pendingReview.Items.ToArray();
    }

    public async Task<IReadOnlyCollection<DepositOutboxMessageItemResponse>> GetDepositOutboxMessagesAsync(
        bool pendingOnly,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("deposit-service");

        try
        {
            return await client.GetFromJsonAsync<DepositOutboxMessageItemResponse[]>(
                       $"api/v1/deposits/outbox?maxCount=50&pendingOnly={pendingOnly.ToString().ToLowerInvariant()}",
                       cancellationToken)
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<DepositRuntimeStatusResponse> GetDepositRuntimeStatusAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("deposit-service");

        try
        {
            return await client.GetFromJsonAsync<DepositRuntimeStatusResponse>(
                       "api/v1/deposits/runtime/status",
                       cancellationToken)
                   ?? new DepositRuntimeStatusResponse(DateTimeOffset.UtcNow, "Unknown", 0, 0, []);
        }
        catch
        {
            return new DepositRuntimeStatusResponse(DateTimeOffset.UtcNow, "Unavailable", 0, 0, []);
        }
    }

    public async Task<DepositWorkflowDetailResponse?> GetDepositWorkflowDetailAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("deposit-service");

        try
        {
            using var response = await client.GetAsync($"api/v1/deposits/{Uri.EscapeDataString(transactionId)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            return MapDepositWorkflowDetail(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    public async Task<CorrelationDiagnosticsResponse> GetCorrelationDiagnosticsAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var depositClient = httpClientFactory.CreateClient("deposit-service");
        var auditClient = httpClientFactory.CreateClient("audit-service");

        var depositsTask = GetDepositsByCorrelationIdAsync(depositClient, correlationId, cancellationToken);
        var auditEventsTask = GetAuditEventsByCorrelationIdAsync(auditClient, correlationId, cancellationToken);

        await Task.WhenAll(depositsTask, auditEventsTask);

        return new CorrelationDiagnosticsResponse(
            correlationId,
            DateTimeOffset.UtcNow,
            depositsTask.Result,
            auditEventsTask.Result);
    }

    public async Task<IReadOnlyCollection<AuditTraceItemResponse>> GetPlatformOperationsAuditAsync(CancellationToken cancellationToken)
    {
        var auditClient = httpClientFactory.CreateClient("audit-service");
        return await GetAuditEventsByActionPrefixAsync(auditClient, "Platform", cancellationToken);
    }

    private static async Task<int> GetDepositCountAsync(HttpClient client, string status, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            $"api/v1/deposits?status={Uri.EscapeDataString(status)}&pageNumber=1&pageSize=1",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        if (document.RootElement.TryGetProperty("totalCount", out var totalCountElement) &&
            totalCountElement.TryGetInt32(out var totalCount))
        {
            return totalCount;
        }

        return 0;
    }

    private static async Task<IReadOnlyCollection<DepositWorkflowDetailResponse>> GetDepositsByCorrelationIdAsync(
        HttpClient client,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(
                $"api/v1/deposits?correlationId={Uri.EscapeDataString(correlationId)}&pageNumber=1&pageSize=20",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var summaries = itemsElement.EnumerateArray()
                .Select(MapDepositSummary)
                .ToArray();

            if (summaries.Length == 0)
            {
                return [];
            }

            var detailTasks = summaries.Select(summary => GetDepositWorkflowDetailAsync(client, summary.TransactionId, cancellationToken));
            var details = await Task.WhenAll(detailTasks);
            return details.Where(detail => detail is not null).Cast<DepositWorkflowDetailResponse>().ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<DepositWorkflowDetailResponse?> GetDepositWorkflowDetailAsync(
        HttpClient client,
        string transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"api/v1/deposits/{Uri.EscapeDataString(transactionId)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            return MapDepositWorkflowDetail(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<IReadOnlyCollection<AuditTraceItemResponse>> GetAuditEventsByCorrelationIdAsync(
        HttpClient client,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync("api/v1/audits?pageNumber=1&pageSize=100", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return itemsElement.EnumerateArray()
                .Select(MapAuditTraceItem)
                .Where(item => string.Equals(item.CorrelationId, correlationId, StringComparison.Ordinal))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyCollection<AuditTraceItemResponse>> GetAuditEventsByActionPrefixAsync(
        HttpClient client,
        string actionPrefix,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync("api/v1/audits?pageNumber=1&pageSize=100", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return itemsElement.EnumerateArray()
                .Select(MapAuditTraceItem)
                .Where(item => item.Action.StartsWith(actionPrefix, StringComparison.Ordinal))
                .OrderByDescending(item => item.OccurredAt)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<int> SafeGetDepositCountAsync(HttpClient client, string status, CancellationToken cancellationToken)
    {
        try
        {
            return await GetDepositCountAsync(client, status, cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<PagedResponse<DepositPendingReviewItemResponse>> SafeGetPendingReviewAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFromJsonAsync<PagedResponse<DepositPendingReviewItemResponse>>(
                       "api/v1/deposits/review/pending?sortBy=ReviewRequiredAt&descending=false&pageNumber=1&pageSize=10",
                       cancellationToken)
                   ?? new PagedResponse<DepositPendingReviewItemResponse>([], 1, 10, 0, 0);
        }
        catch
        {
            return new PagedResponse<DepositPendingReviewItemResponse>([], 1, 10, 0, 0);
        }
    }

    private async Task<PlatformCompatibilityStatusResponse> BuildCompatibilityStatusAsync(
        PlatformServiceStatusResponse service,
        CancellationToken cancellationToken)
    {
        var expectedPaths = GetExpectedCriticalPaths(service.Name);
        var checkedAt = DateTimeOffset.UtcNow;
        var httpClient = httpClientFactory.CreateClient($"{service.Name}-service");

        try
        {
            using var response = await httpClient.GetAsync("openapi/v1.json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PlatformCompatibilityStatusResponse(
                    service.Name,
                    hostEnvironment.EnvironmentName,
                    "PublicServiceContract",
                    "openapi-phase1",
                    service.OpenApiUrl,
                    false,
                    "Unavailable",
                    $"Runtime OpenAPI returned {(int)response.StatusCode}.",
                    null,
                    null,
                    0,
                    expectedPaths.Count,
                    expectedPaths.Count,
                    expectedPaths,
                    $"HTTP {(int)response.StatusCode}",
                    checkedAt);
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            var info = root.TryGetProperty("info", out var infoElement) ? infoElement : default;
            var runtimeTitle = info.ValueKind == JsonValueKind.Object && info.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;
            var runtimeVersion = info.ValueKind == JsonValueKind.Object && info.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString()
                : null;

            var runtimePaths = root.TryGetProperty("paths", out var pathsElement) && pathsElement.ValueKind == JsonValueKind.Object
                ? pathsElement.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal)
                : [];

            var missingPaths = expectedPaths
                .Where(path => !runtimePaths.Contains(path))
                .ToArray();

            var status = missingPaths.Length == 0 ? "Compatible" : "DriftDetected";
            var driftSummary = missingPaths.Length == 0
                ? $"Runtime OpenAPI includes all {expectedPaths.Count} critical baseline paths."
                : $"Missing {missingPaths.Length} of {expectedPaths.Count} critical baseline paths.";

            return new PlatformCompatibilityStatusResponse(
                service.Name,
                hostEnvironment.EnvironmentName,
                "PublicServiceContract",
                "openapi-phase1",
                service.OpenApiUrl,
                true,
                status,
                driftSummary,
                runtimeTitle,
                runtimeVersion,
                runtimePaths.Count,
                expectedPaths.Count,
                missingPaths.Length,
                missingPaths,
                null,
                checkedAt);
        }
        catch (Exception exception)
        {
            return new PlatformCompatibilityStatusResponse(
                service.Name,
                hostEnvironment.EnvironmentName,
                "PublicServiceContract",
                "openapi-phase1",
                service.OpenApiUrl,
                false,
                "Unavailable",
                "Runtime OpenAPI could not be fetched or parsed.",
                null,
                null,
                0,
                expectedPaths.Count,
                expectedPaths.Count,
                expectedPaths,
                exception.Message,
                checkedAt);
        }
    }

    private static IReadOnlyCollection<string> GetExpectedCriticalPaths(string serviceName)
    {
        return serviceName switch
        {
            "customer" =>
            [
                "/api/v1/customers",
                "/api/v1/customers/{customerId}",
                "/api/v1/customers/{customerId}/status"
            ],
            "account" =>
            [
                "/api/v1/accounts",
                "/api/v1/accounts/{accountId}",
                "/api/v1/accounts/by-number/{accountNumber}",
                "/api/v1/accounts/{accountId}/activities"
            ],
            "deposit" =>
            [
                "/api/v1/deposits",
                "/api/v1/deposits/{transactionId}",
                "/api/v1/deposits/review/pending",
                "/api/v1/deposits/{transactionId}/review/retry-compensation",
                "/api/v1/deposits/{transactionId}/review/resolve"
            ],
            "audit" =>
            [
                "/api/v1/audits",
                "/api/v1/audits/{auditId}"
            ],
            _ => []
        };
    }

    private static DepositWorkflowDetailResponse MapDepositWorkflowDetail(JsonElement element)
        => new(
            element.GetProperty("transactionId").GetString() ?? string.Empty,
            element.GetProperty("transactionNumber").GetString() ?? string.Empty,
            element.GetProperty("customerId").GetString() ?? string.Empty,
            element.GetProperty("accountId").GetString() ?? string.Empty,
            element.GetProperty("amount").GetDecimal(),
            element.GetProperty("currency").GetString() ?? string.Empty,
            element.TryGetProperty("referenceNumber", out var referenceNumber) && referenceNumber.ValueKind != JsonValueKind.Null
                ? referenceNumber.GetString()
                : null,
            element.GetProperty("channel").GetString() ?? string.Empty,
            element.GetProperty("status").GetString() ?? string.Empty,
            element.GetProperty("accountPostingStatus").GetString() ?? string.Empty,
            element.GetProperty("auditStatus").GetString() ?? string.Empty,
            element.GetProperty("compensationStatus").GetString() ?? string.Empty,
            element.GetProperty("reviewResolution").GetString() ?? string.Empty,
            element.GetProperty("correlationId").GetString() ?? string.Empty,
            element.TryGetProperty("failureCode", out var failureCode) && failureCode.ValueKind != JsonValueKind.Null
                ? failureCode.GetString()
                : null,
            element.TryGetProperty("failureReason", out var failureReason) && failureReason.ValueKind != JsonValueKind.Null
                ? failureReason.GetString()
                : null,
            element.GetProperty("compensationRetryCount").GetInt32(),
            element.TryGetProperty("reviewLastActionBy", out var reviewLastActionBy) && reviewLastActionBy.ValueKind != JsonValueKind.Null
                ? reviewLastActionBy.GetString()
                : null,
            element.TryGetProperty("reviewNote", out var reviewNote) && reviewNote.ValueKind != JsonValueKind.Null
                ? reviewNote.GetString()
                : null,
            element.GetProperty("requestedAt").GetDateTimeOffset(),
            GetOptionalDateTimeOffset(element, "postedAt"),
            GetOptionalDateTimeOffset(element, "reversedAt"),
            GetOptionalDateTimeOffset(element, "reviewRequiredAt"),
            GetOptionalDateTimeOffset(element, "reviewResolvedAt"),
            GetOptionalDateTimeOffset(element, "lastCompensationAttemptAt"),
            GetOptionalDateTimeOffset(element, "lastProcessedAt"));

    private static (string TransactionId, string CorrelationId) MapDepositSummary(JsonElement element)
        => (
            element.GetProperty("transactionId").GetString() ?? string.Empty,
            element.TryGetProperty("correlationId", out var correlationId) && correlationId.ValueKind != JsonValueKind.Null
                ? correlationId.GetString() ?? string.Empty
                : string.Empty);

    private static AuditTraceItemResponse MapAuditTraceItem(JsonElement element)
        => new(
            element.GetProperty("auditId").GetString() ?? string.Empty,
            element.GetProperty("actorType").GetString() ?? string.Empty,
            element.GetProperty("actorId").GetString() ?? string.Empty,
            element.GetProperty("action").GetString() ?? string.Empty,
            element.GetProperty("aggregateType").GetString() ?? string.Empty,
            element.GetProperty("aggregateId").GetString() ?? string.Empty,
            element.GetProperty("correlationId").GetString() ?? string.Empty,
            element.GetProperty("occurredAt").GetDateTimeOffset());

    private static DateTimeOffset? GetOptionalDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.GetDateTimeOffset();
    }
}
