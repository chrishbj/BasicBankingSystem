namespace Banking.Gateway.Contracts;

public sealed record GatewayServiceStatusResponse(
    string Name,
    string BasePath,
    string Health,
    int? StatusCode,
    string SwaggerUrl,
    string OpenApiUrl);

public sealed record GatewayHealthSummaryResponse(
    string Gateway,
    DateTimeOffset CheckedAt,
    IReadOnlyList<GatewayServiceStatusResponse> Services);
