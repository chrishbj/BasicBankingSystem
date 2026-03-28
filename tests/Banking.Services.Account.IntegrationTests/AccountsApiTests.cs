using System.Net;
using System.Net.Http.Json;
using Banking.Services.Account.Contracts;
using FluentAssertions;

namespace Banking.Services.Account.IntegrationTests;

public sealed class AccountsApiTests : IClassFixture<AccountServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountsApiTests(AccountServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAccounts_Should_ReturnCreated_When_CustomerIsActive()
    {
        var request = new OpenAccountRequest("cus_active_001", "Checking", "CNY");

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.CustomerId.Should().Be("cus_active_001");
    }

    [Fact]
    public async Task PostAccounts_Should_ReturnConflict_When_CustomerIsFrozen()
    {
        var request = new OpenAccountRequest("cus_frozen_001", "Checking", "CNY");

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostDepositPosting_Should_UpdateBalances_When_AccountIsActive()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", new OpenAccountRequest("cus_active_001", "Checking", "CNY"));
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            new ApplyDepositRequest(125m, "CNY"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(125m);
        updated.LedgerBalance.Should().Be(125m);
    }
}
