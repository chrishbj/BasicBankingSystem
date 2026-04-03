using Banking.Services.Customer.Contracts;

namespace Banking.Services.Customer.IntegrationTests.Support;

internal static class CustomerApiTestData
{
    public static CreateCustomerRequest CreateCustomer(string fullName = "Alice Teller")
    {
        var token = Guid.NewGuid().ToString("N");
        return new CreateCustomerRequest(
            fullName,
            "NationalId",
            token,
            $"138{Random.Shared.Next(10000000, 99999999)}",
            $"{token[..8]}@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");
    }

    public static CreateCustomerRequest CreatePortalCustomer(string identityLast4 = "0023")
    {
        var token = Random.Shared.Next(1000, 9999);
        return new CreateCustomerRequest(
            "Portal Sign In",
            "NationalId",
            $"WEB-177475688{token}{identityLast4}",
            $"138{Random.Shared.Next(10000000, 99999999)}",
            $"portal-{Guid.NewGuid():N}@example.com",
            new AddressRequest("US", "New York", "New York", "2 Demo Plaza", "10001"),
            "Low");
    }
}
