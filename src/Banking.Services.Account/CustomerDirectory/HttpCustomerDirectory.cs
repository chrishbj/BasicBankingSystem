using System.Net;
using System.Net.Http.Json;

namespace Banking.Services.Account.CustomerDirectory;

public sealed class HttpCustomerDirectory(HttpClient httpClient) : ICustomerDirectory
{
    public async Task<CustomerDirectoryRecord?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/api/v1/customers/{customerId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(cancellationToken);
        if (customer is null)
        {
            return null;
        }

        return new CustomerDirectoryRecord(customer.CustomerId, customer.Status);
    }
}
