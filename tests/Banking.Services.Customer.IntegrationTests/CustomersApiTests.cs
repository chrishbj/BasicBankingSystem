using System.Net;
using System.Net.Http.Json;
using Banking.Services.Customer.Contracts;
using Banking.Services.Customer.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Customer.IntegrationTests;

public sealed class CustomersApiTests : IClassFixture<CustomerServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomersApiTests(CustomerServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCustomers_Should_ReturnCreated_When_RequestIsValid()
    {
        var request = CustomerApiTestData.CreateCustomer();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.FullName.Should().Be("Alice Teller");
    }

    [Fact]
    public async Task PostCustomers_Should_ReturnConflict_When_MobileExists()
    {
        var mobile = $"138{Random.Shared.Next(10000000, 99999999)}";

        var first = new CreateCustomerRequest(
            "Alice Teller",
            "NationalId",
            Guid.NewGuid().ToString("N"),
            mobile,
            "alice@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");

        var second = new CreateCustomerRequest(
            "Bob Teller",
            "NationalId",
            Guid.NewGuid().ToString("N"),
            mobile,
            "bob@example.com",
            new AddressRequest("CN", "Shanghai", "Shanghai", "No.2 Road", "200000"),
            "Low");

        await _client.PostAsJsonAsync("/api/v1/customers", first);
        var response = await _client.PostAsJsonAsync("/api/v1/customers", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostCustomers_Should_ReturnConflictProblemDetails_When_IdentityExists()
    {
        var identityNumber = Guid.NewGuid().ToString("N");
        var first = new CreateCustomerRequest(
            "Alice Teller",
            "NationalId",
            identityNumber,
            $"138{Random.Shared.Next(10000000, 99999999)}",
            "alice.identity@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");

        var second = new CreateCustomerRequest(
            "Bob Teller",
            "NationalId",
            identityNumber,
            $"139{Random.Shared.Next(10000000, 99999999)}",
            "bob.identity@example.com",
            new AddressRequest("CN", "Shanghai", "Shanghai", "No.2 Road", "200000"),
            "Low");

        await _client.PostAsJsonAsync("/api/v1/customers", first);
        var response = await _client.PostAsJsonAsync("/api/v1/customers", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Duplicate customer");
        problem.Detail.Should().Contain("Identity number already exists");
    }

    [Fact]
    public async Task GetCustomerById_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/v1/customers/{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PortalSignIn_Should_ReturnOk_When_CredentialsMatch()
    {
        var createRequest = CustomerApiTestData.CreatePortalCustomer();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var signInResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers/portal-sign-in",
            new CustomerPortalSignInRequest(created!.CustomerNumber, "0023"));

        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var signedIn = await signInResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        signedIn.Should().NotBeNull();
        signedIn!.CustomerNumber.Should().Be(created.CustomerNumber);
    }

    [Fact]
    public async Task PortalSignIn_Should_ReturnUnauthorized_When_CredentialsDoNotMatch()
    {
        var createRequest = CustomerApiTestData.CreatePortalCustomer();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var signInResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers/portal-sign-in",
            new CustomerPortalSignInRequest(created!.CustomerNumber, "9999"));

        signInResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await signInResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Invalid sign-in details");
    }

    [Fact]
    public async Task ChangeStatus_Should_ReturnBadRequestProblemDetails_When_TransitionIsInvalid()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers",
            CustomerApiTestData.CreateCustomer("Status Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{created!.CustomerId}/status",
            new ChangeCustomerStatusRequest(Banking.Services.Customer.Domain.CustomerStatus.Closed, "invalid jump"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Invalid status transition");
    }

    [Fact]
    public async Task GetCustomers_Should_ReturnEmptyItems_When_PageIsOutOfRange()
    {
        await _client.PostAsJsonAsync("/api/v1/customers", CustomerApiTestData.CreateCustomer("Paged Customer"));

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<CustomerSummaryResponse>>(
            "/api/v1/customers?pageNumber=2&pageSize=20");

        response.Should().NotBeNull();
        response!.TotalCount.Should().BeGreaterThan(0);
        response.Items.Should().BeEmpty();
    }
}
