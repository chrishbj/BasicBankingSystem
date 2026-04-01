using System.Net.Http.Json;
using Banking.Bff.CustomerPortal.Contracts;

namespace Banking.Bff.CustomerPortal.Clients;

public sealed class CustomerServiceClient(HttpClient httpClient)
{
    public async Task<CustomerResponse> SignInAsync(CustomerPortalSignInRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/customers/portal-sign-in", request, cancellationToken);
        return await ReadRequiredAsync<CustomerResponse>(response, cancellationToken);
    }

    public async Task<CustomerResponse> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/v1/customers/{customerId}", cancellationToken);
        return await ReadRequiredAsync<CustomerResponse>(response, cancellationToken);
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
