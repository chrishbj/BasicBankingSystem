using System.Net;
using System.Net.Http.Json;
using Banking.Services.Account.Contracts;
using Banking.Services.Account.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

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
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.CustomerId.Should().Be("cus_active_001");
    }

    [Fact]
    public async Task PostAccounts_Should_ReturnConflict_When_CustomerIsFrozen()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new OpenAccountRequest("cus_frozen_001", "Checking", "USD"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Customer is not eligible for account opening");
    }

    [Fact]
    public async Task GetAccountById_Should_ReturnNotFound_When_AccountDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/acc_missing_{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostDepositPosting_Should_UpdateBalances_When_AccountIsActive()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            AccountApiTestData.CreateDeposit(125m, "posting-001", "corr-001"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(125m);
        updated.LedgerBalance.Should().Be(125m);
    }

    [Fact]
    public async Task GetByAccountNumber_Should_ReturnAccount_When_NumberExists()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await _client.GetAsync($"/api/v1/accounts/by-number/{account!.AccountNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<AccountResponse>();
        fetched.Should().NotBeNull();
        fetched!.AccountId.Should().Be(account.AccountId);
    }

    [Fact]
    public async Task PostDepositReversal_Should_RollbackBalances_When_OriginalPostingExists()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            AccountApiTestData.CreateDeposit(125m, "posting-002", "corr-002"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/deposit-reversals",
            new ReverseDepositRequest("reversal-002", "posting-002", 125m, "USD", "corr-002", "test compensation"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(0m);
        updated.LedgerBalance.Should().Be(0m);
    }

    [Fact]
    public async Task PostWithdrawal_Should_UpdateBalances_When_AccountHasSufficientFunds()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            AccountApiTestData.CreateDeposit(200m, "posting-003", "corr-003"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            AccountApiTestData.CreateWithdrawal(50m, "withdrawal-003", "corr-004"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountResponse>();
        updated.Should().NotBeNull();
        updated!.AvailableBalance.Should().Be(150m);
        updated.LedgerBalance.Should().Be(150m);
    }

    [Fact]
    public async Task PostWithdrawal_Should_ReturnConflictProblemDetails_When_BalanceIsInsufficient()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/withdrawals",
            AccountApiTestData.CreateWithdrawal(50m, "withdrawal-insufficient-001", "corr-insufficient-001"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Account is not eligible for withdrawal");
        problem.Detail.Should().Contain("Insufficient balance");
    }

    [Fact]
    public async Task GetActivities_Should_ReturnDepositAndWithdrawalHistory()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            AccountApiTestData.CreateDeposit(200m, "posting-activity-001", "corr-activity-001"));

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            AccountApiTestData.CreateWithdrawal(60m, "withdrawal-activity-001", "corr-activity-002", "branch withdrawal"));

        var response = await _client.GetAsync($"/api/v1/accounts/{account.AccountId}/activities?pageNumber=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var activities = await response.Content.ReadFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AccountActivityResponse>>();
        activities.Should().NotBeNull();
        activities!.Items.Should().Contain(x => x.PostingType == Banking.Services.Account.Domain.AccountPostingType.DepositCredit);
        activities.Items.Should().Contain(x => x.PostingType == Banking.Services.Account.Domain.AccountPostingType.WithdrawalDebit);
    }

    [Fact]
    public async Task GetActivities_Should_RespectPagingAndTypeFilter()
    {
        var openResponse = await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());
        var account = await openResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account!.AccountId}/deposit-postings",
            AccountApiTestData.CreateDeposit(200m, $"posting-filter-{Guid.NewGuid():N}", "corr-filter-001"));

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            AccountApiTestData.CreateWithdrawal(30m, $"withdrawal-filter-a-{Guid.NewGuid():N}", "corr-filter-002", "branch withdrawal"));

        await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{account.AccountId}/withdrawals",
            AccountApiTestData.CreateWithdrawal(20m, $"withdrawal-filter-b-{Guid.NewGuid():N}", "corr-filter-003", "branch withdrawal"));

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AccountActivityResponse>>(
            $"/api/v1/accounts/{account.AccountId}/activities?activityType=WithdrawalDebit&pageNumber=2&pageSize=1");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(2);
        response.TotalPages.Should().Be(2);
        response.Items.Should().ContainSingle();
        response.Items.Single().PostingType.Should().Be(Banking.Services.Account.Domain.AccountPostingType.WithdrawalDebit);
    }

    [Fact]
    public async Task GetAccountsByCustomer_Should_ReturnEmptyItems_When_PageIsOutOfRange()
    {
        await _client.PostAsJsonAsync("/api/v1/accounts", AccountApiTestData.OpenActiveCheckingAccount());

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AccountSummaryResponse>>(
            "/api/v1/accounts?customerId=cus_active_001&pageNumber=2&pageSize=20");

        response.Should().NotBeNull();
        response!.TotalCount.Should().BeGreaterThan(0);
        response.Items.Should().BeEmpty();
    }
}
