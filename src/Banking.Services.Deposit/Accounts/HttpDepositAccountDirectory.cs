using System.Net;
using System.Net.Http.Json;

namespace Banking.Services.Deposit.Accounts;

public sealed class HttpDepositAccountDirectory(HttpClient httpClient) : IDepositAccountDirectory
{
    public async Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/api/v1/accounts/{accountId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>(cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new DepositAccountRecord
        {
            AccountId = account.AccountId,
            CustomerId = account.CustomerId,
            Currency = account.Currency,
            Status = MapStatus(account.Status),
            AvailableBalance = account.AvailableBalance,
            LedgerBalance = account.LedgerBalance
        };
    }

    public async Task PostDepositAsync(string accountId, decimal amount, string currency, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposit-postings",
            new ApplyDepositRequest(amount, currency),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Account '{accountId}' was not found.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var problem = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(problem);
        }

        response.EnsureSuccessStatusCode();
    }

    private static DepositAccountStatus MapStatus(int status)
    {
        return status switch
        {
            1 => DepositAccountStatus.Active,
            2 => DepositAccountStatus.Frozen,
            3 => DepositAccountStatus.Closed,
            _ => DepositAccountStatus.Frozen
        };
    }
}
