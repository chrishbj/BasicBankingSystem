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
}
