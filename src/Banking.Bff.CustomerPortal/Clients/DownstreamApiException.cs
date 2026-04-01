namespace Banking.Bff.CustomerPortal.Clients;

public sealed class DownstreamApiException(int statusCode, string title, string? detail = null) : Exception(detail ?? title)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;

    public string? Detail { get; } = detail;
}
