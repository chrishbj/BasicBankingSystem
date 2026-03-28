using Banking.Services.Customer.Contracts;
using Banking.Services.Customer.Domain;
using Banking.Services.Customer.Exceptions;
using Banking.Services.Customer.Repositories;
using Banking.Services.Customer.Services;
using FluentAssertions;

namespace Banking.Services.Customer.UnitTests;

public sealed class CustomerServiceTests
{
    private readonly ICustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(new InMemoryCustomerRepository());
    }

    [Fact]
    public async Task CreateCustomer_Should_Succeed_When_RequestIsValid()
    {
        var request = new CreateCustomerRequest(
            "Alice Teller",
            "NationalId",
            "110101199001011234",
            "13800000001",
            "alice@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");

        var customer = await _service.CreateAsync(request, CancellationToken.None);

        customer.CustomerId.Should().NotBeNullOrWhiteSpace();
        customer.CustomerNumber.Should().StartWith("C");
        customer.Status.Should().Be(CustomerStatus.Pending);
    }

    [Fact]
    public async Task CreateCustomer_Should_Fail_When_MobileExists()
    {
        var request = new CreateCustomerRequest(
            "Alice Teller",
            "NationalId",
            "110101199001011234",
            "13800000001",
            "alice@example.com",
            new AddressRequest("CN", "Beijing", "Beijing", "No.1 Road", "100000"),
            "Low");

        await _service.CreateAsync(request, CancellationToken.None);

        var act = () => _service.CreateAsync(
            request with { IdentityNumber = "220202199001011234" },
            CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCustomerException>();
    }
}
