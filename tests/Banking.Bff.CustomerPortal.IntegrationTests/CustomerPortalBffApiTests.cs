using System.Net;
using System.Net.Http.Json;
using Banking.Bff.CustomerPortal.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Banking.Bff.CustomerPortal.IntegrationTests;

public sealed class CustomerPortalBffApiTests : IClassFixture<CustomerPortalBffWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomerPortalBffApiTests(CustomerPortalBffWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task GetDashboard_Should_ReturnUnauthorized_When_UserIsNotSignedIn()
    {
        var response = await _client.GetAsync("/api/customer-portal/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignIn_Then_Me_Should_CreateCustomerSession()
    {
        var signInResponse = await _client.PostAsJsonAsync(
            "/api/customer-portal/auth/sign-in",
            new CustomerPortalSignInRequest(CustomerPortalBffWebApplicationFactory.CustomerNumber, "0001"));

        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var signedIn = await signInResponse.Content.ReadFromJsonAsync<PortalCustomerResponse>();
        signedIn.Should().NotBeNull();
        signedIn!.CustomerNumber.Should().Be(CustomerPortalBffWebApplicationFactory.CustomerNumber);
        signedIn.PortalIdentityLast4.Should().Be("0001");

        var meResponse = await _client.GetAsync("/api/customer-portal/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<PortalCustomerResponse>();
        me.Should().NotBeNull();
        me!.CustomerNumber.Should().Be(CustomerPortalBffWebApplicationFactory.CustomerNumber);
    }

    [Fact]
    public async Task SignIn_Should_ReturnUnauthorizedProblemDetails_When_CredentialsAreInvalid()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/customer-portal/auth/sign-in",
            new CustomerPortalSignInRequest(CustomerPortalBffWebApplicationFactory.CustomerNumber, "9999"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Invalid portal sign-in");
    }

    [Fact]
    public async Task Dashboard_Should_ReturnAggregatedCustomerScopedView()
    {
        await SignInAsync();

        var response = await _client.GetFromJsonAsync<CustomerDashboardResponse>("/api/customer-portal/dashboard");

        response.Should().NotBeNull();
        response!.Customer.CustomerNumber.Should().Be(CustomerPortalBffWebApplicationFactory.CustomerNumber);
        response.Portfolio.AccountCount.Should().Be(1);
        response.CurrentAccount.Should().NotBeNull();
        response.CurrentAccount!.AccountNumber.Should().Be(CustomerPortalBffWebApplicationFactory.AccountNumber);
        response.RecentActivities.Should().Contain(item => item.Type == "Withdrawal");
        response.RecentTransactions.Should().ContainSingle(item => item.TransactionNumber == "D202603311420531889");
    }

    [Fact]
    public async Task SignOut_Should_ClearCustomerSession()
    {
        await SignInAsync();

        var signOutResponse = await _client.PostAsync("/api/customer-portal/auth/sign-out", null);
        signOutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meResponse = await _client.GetAsync("/api/customer-portal/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Transactions_Should_UseBusinessIdentifiers_Only()
    {
        await SignInAsync();

        var response = await _client.GetAsync($"/api/customer-portal/transactions?accountNumber={CustomerPortalBffWebApplicationFactory.AccountNumber}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(CustomerPortalBffWebApplicationFactory.AccountNumber);
        content.Should().Contain("transactionNumber");
        content.Should().NotContain("transactionId");
        content.Should().NotContain("accountId");

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<TransactionStatusSummaryResponse>>();
        payload.Should().NotBeNull();
        payload!.Items.Should().OnlyContain(item => item.AccountNumber == CustomerPortalBffWebApplicationFactory.AccountNumber);
    }

    [Fact]
    public async Task AccountLookup_Should_ReturnForbidden_When_AccountBelongsToAnotherCustomer()
    {
        await SignInAsync();

        var response = await _client.GetAsync($"/api/customer-portal/accounts/{CustomerPortalBffWebApplicationFactory.ForeignAccountNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> SignInAsync() =>
        _client.PostAsJsonAsync(
            "/api/customer-portal/auth/sign-in",
            new CustomerPortalSignInRequest(CustomerPortalBffWebApplicationFactory.CustomerNumber, "0001"));

    private sealed record PortalCustomerResponse(
        string CustomerNumber,
        string FullName,
        string IdentityType,
        string IdentityNumberMasked,
        string PortalIdentityLast4,
        string Mobile,
        string? Email,
        string RiskLevel,
        int Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
