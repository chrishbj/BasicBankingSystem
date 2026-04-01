using Banking.Gateway.Contracts;

namespace Banking.Gateway.Services;

public sealed class GatewayHealthService(IHttpClientFactory httpClientFactory)
{
    private static readonly (string Name, string ClientName, string BasePath)[] ServiceMap =
    [
        ("customer", "customer-service", "/customer-api"),
        ("account", "account-service", "/account-api"),
        ("deposit", "deposit-service", "/deposit-api"),
        ("audit", "audit-service", "/audit-api")
    ];

    public async Task<GatewayHealthSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(ServiceMap.Select(service => CheckServiceAsync(service, cancellationToken)));

        return new GatewayHealthSummaryResponse(
            "Banking.Gateway",
            DateTimeOffset.UtcNow,
            results);
    }

    private async Task<GatewayServiceStatusResponse> CheckServiceAsync(
        (string Name, string ClientName, string BasePath) service,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(service.ClientName);
            using var response = await client.GetAsync("api/v1/health", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var health = response.IsSuccessStatusCode ? body : "Unavailable";

            return new GatewayServiceStatusResponse(
                service.Name,
                service.BasePath,
                string.IsNullOrWhiteSpace(health) ? "Healthy" : health,
                (int)response.StatusCode,
                $"{service.BasePath}/swagger",
                $"{service.BasePath}/openapi/v1.json");
        }
        catch (TaskCanceledException)
        {
            return BuildUnavailable(service, "Timed out");
        }
        catch (Exception)
        {
            return BuildUnavailable(service, "Unavailable");
        }
    }

    private static GatewayServiceStatusResponse BuildUnavailable(
        (string Name, string ClientName, string BasePath) service,
        string health) =>
        new(
            service.Name,
            service.BasePath,
            health,
            null,
            $"{service.BasePath}/swagger",
            $"{service.BasePath}/openapi/v1.json");
}
