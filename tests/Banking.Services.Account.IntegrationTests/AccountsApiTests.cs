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
            new ApplyDepositRequest(125m, "CNY", "posting-001", "corr-001"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(125m);
        updated.LedgerBalance.Should().Be(125m);
    }

    [Fact]
    public async Task PostDepositReversal_Should_RollbackBalances_When_OriginalPostingExists()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", new OpenAccountRequest("cus_active_001", "Checking", "CNY"));
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            new ApplyDepositRequest(125m, "CNY", "posting-002", "corr-002"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/deposit-reversals",
            new ReverseDepositRequest("reversal-002", "posting-002", 125m, "CNY", "corr-002", "test compensation"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(0m);
        updated.LedgerBalance.Should().Be(0m);
    }

    [Fact]
    public async Task PostWithdrawal_Should_UpdateBalances_When_AccountHasSufficientFunds()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", new OpenAccountRequest("cus_active_001", "Checking", "CNY"));
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            new ApplyDepositRequest(200m, "CNY", "posting-003", "corr-003"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            new CreateWithdrawalRequest(50m, "CNY", "withdrawal-003", "corr-004", "atm withdrawal"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(150m);
        updated.LedgerBalance.Should().Be(150m);
    }

    [Fact]
    public async Task GetActivities_Should_ReturnDepositAndWithdrawalHistory()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", new OpenAccountRequest("cus_active_001", "Checking", "CNY"));
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            new ApplyDepositRequest(200m, "CNY", "posting-activity-001", "corr-activity-001"));

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            new CreateWithdrawalRequest(60m, "CNY", "withdrawal-activity-001", "corr-activity-002", "branch withdrawal"));

        var response = await _client.GetAsync($"/api/v1/accounts/{account.AccountId}/activities?pageNumber=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var activities = await response.Content.ReadFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AccountActivityResponse>>();
        activities.Should().NotBeNull();
        activities!.Items.Should().Contain(x => x.PostingType == Banking.Services.Account.Domain.AccountPostingType.DepositCredit);
        activities.Items.Should().Contain(x => x.PostingType == Banking.Services.Account.Domain.AccountPostingType.WithdrawalDebit);
    }
}
