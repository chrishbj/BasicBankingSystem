using System.Net.Http.Json;
using Banking.Bff.CustomerPortal.Contracts;

namespace Banking.Bff.CustomerPortal.Clients;

public sealed class DepositServiceClient(HttpClient httpClient)
{
    public async Task<DepositResponse> CreateAsync(
        string customerId,
        string accountId,
        CreatePortalDepositRequest request,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/deposits")
        {
            Content = JsonContent.Create(new
            {
                customerId,
                accountId,
                request.Amount,
                request.Currency,
                request.Channel,
                request.ReferenceNumber,
                request.Note
            })
        };
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredAsync<DepositResponse>(response, cancellationToken);
    }

    public async Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/deposits/{transactionId}", cancellationToken);
        return await ReadRequiredAsync<DepositResponse>(response, cancellationToken);
    }

    public async Task<PagedResponse<DepositSummaryResponse>> SearchAsync(string customerId, string? accountId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = $"customerId={Uri.EscapeDataString(customerId)}&pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query += $"&accountId={Uri.EscapeDataString(accountId)}";
        }

        using var response = await httpClient.GetAsync($"api/v1/deposits?{query}", cancellationToken);
        return await ReadRequiredAsync<PagedResponse<DepositSummaryResponse>>(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(response, cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("Downstream service returned an empty response.");
    }

    private static async Task<DownstreamApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var details = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(cancellationToken: cancellationToken);
        return new DownstreamApiException((int)response.StatusCode, details?.Title ?? response.ReasonPhrase ?? "Downstream request failed", details?.Detail);
    }
}
