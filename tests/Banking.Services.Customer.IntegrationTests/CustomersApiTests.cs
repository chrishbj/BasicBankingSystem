using System.Net;
using System.Net.Http.Json;
using Banking.Services.Customer.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Banking.Services.Customer.IntegrationTests;

public sealed class CustomersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CustomersApiTests(WebApplicationFactory<Program> factory)
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
}
