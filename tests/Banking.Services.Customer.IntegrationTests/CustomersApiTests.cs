using System.Net;
using System.Net.Http.Json;
using Banking.Services.Customer.Contracts;
using FluentAssertions;

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
        var request = new CreateCustomerRequest(
            "Alice Teller",
            "NationalId",
            Guid.NewGuid().ToString("N"),
            $"138{Random.Shared.Next(10000000, 99999999)}",
            "alice@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");

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
    public async Task PortalSignIn_Should_ReturnOk_When_CredentialsMatch()
    {
        var identityNumber = $"WEB-177475688{Random.Shared.Next(1000, 9999)}0023";
        var createRequest = new CreateCustomerRequest(
            "Portal Sign In",
            "NationalId",
            identityNumber,
            $"138{Random.Shared.Next(10000000, 99999999)}",
            "portal-sign-in@example.com",
            new AddressRequest("US", "New York", "New York", "2 Demo Plaza", "10001"),
            "Low");

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
        var identityNumber = $"WEB-177475688{Random.Shared.Next(1000, 9999)}0023";
        var createRequest = new CreateCustomerRequest(
            "Portal Sign In",
            "NationalId",
            identityNumber,
            $"138{Random.Shared.Next(10000000, 99999999)}",
            "portal-sign-in-2@example.com",
            new AddressRequest("US", "New York", "New York", "3 Demo Plaza", "10001"),
            "Low");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var signInResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers/portal-sign-in",
            new CustomerPortalSignInRequest(created!.CustomerNumber, "9999"));

        signInResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
