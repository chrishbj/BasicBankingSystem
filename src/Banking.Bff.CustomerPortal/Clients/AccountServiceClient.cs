using System.Net.Http.Json;
using Banking.Bff.CustomerPortal.Contracts;

namespace Banking.Bff.CustomerPortal.Clients;

public sealed class AccountServiceClient(HttpClient httpClient)
{
    public async Task<AccountResponse> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/accounts/by-number/{Uri.EscapeDataString(accountNumber)}", cancellationToken);
        return await ReadRequiredAsync<AccountResponse>(response, cancellationToken);
    }

    public async Task<AccountResponse> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/accounts/{accountId}", cancellationToken);
        return await ReadRequiredAsync<AccountResponse>(response, cancellationToken);
    }

    public async Task<PagedResponse<AccountSummaryResponse>> GetByCustomerIdAsync(string customerId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/accounts?customerId={Uri.EscapeDataString(customerId)}&pageNumber={pageNumber}&pageSize={pageSize}", cancellationToken);
        return await ReadRequiredAsync<PagedResponse<AccountSummaryResponse>>(response, cancellationToken);
    }

    public async Task<PagedResponse<AccountActivityResponse>> GetActivitiesAsync(string accountId, string queryString, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/accounts/{accountId}/activities{queryString}", cancellationToken);
        return await ReadRequiredAsync<PagedResponse<AccountActivityResponse>>(response, cancellationToken);
    }

    public async Task<AccountResponse> WithdrawAsync(string accountId, CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/v1/accounts/{accountId}/withdrawals", request, cancellationToken);
        return await ReadRequiredAsync<AccountResponse>(response, cancellationToken);
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
