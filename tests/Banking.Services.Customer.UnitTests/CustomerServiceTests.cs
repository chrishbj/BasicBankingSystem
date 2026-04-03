using Banking.Services.Customer.Contracts;
using Banking.Services.Customer.Domain;
using Banking.Services.Customer.Exceptions;
using Banking.Services.Customer.Repositories;
using Banking.Services.Customer.Services;
using FluentAssertions;
using Moq;

namespace Banking.Services.Customer.UnitTests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_Should_AddTrimmedPendingCustomer_When_RequestIsValid()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        Domain.Customer? savedCustomer = null;

        repository
            .Setup(item => item.ExistsByIdentityAsync(" NationalId ", " 110101199001011234 ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.ExistsByMobileAsync(" 13800000001 ", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.AddAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Customer, CancellationToken>((customer, _) => savedCustomer = customer)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository);
        var request = CreateRequest();

        var customer = await service.CreateAsync(request, CancellationToken.None);

        customer.CustomerId.Should().StartWith("cus_");
        customer.CustomerNumber.Should().StartWith("C");
        customer.Status.Should().Be(CustomerStatus.Pending);

        savedCustomer.Should().NotBeNull();
        savedCustomer!.FullName.Should().Be("Alice Teller");
        savedCustomer.IdentityType.Should().Be("NationalId");
        savedCustomer.IdentityNumber.Should().Be("110101199001011234");
        savedCustomer.Mobile.Should().Be("13800000001");
        savedCustomer.Email.Should().Be("alice@example.com");
        savedCustomer.Address.Country.Should().Be("CN");
        savedCustomer.Address.City.Should().Be("Beijing");
        savedCustomer.Status.Should().Be(CustomerStatus.Pending);

        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateCustomer_Should_Fail_When_IdentityExists()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.ExistsByIdentityAsync(" NationalId ", " 110101199001011234 ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(repository);

        var act = () => service.CreateAsync(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCustomerException>()
            .WithMessage("*Identity number already exists.*");

        repository.Verify(item => item.ExistsByMobileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateCustomer_Should_Fail_When_MobileExists()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.ExistsByIdentityAsync(" NationalId ", " 110101199001011234 ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.ExistsByMobileAsync(" 13800000001 ", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(repository);

        var act = () => service.CreateAsync(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCustomerException>()
            .WithMessage("*Mobile already exists.*");

        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetById_Should_Throw_When_CustomerDoesNotExist()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("missing-customer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Customer?)null);

        var service = CreateService(repository);

        var act = () => service.GetByIdAsync("missing-customer", CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotFoundException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_ReturnPagedMaskedSummaries()
    {
        var firstCreatedAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var secondCreatedAt = new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero);

        var customers = new[]
        {
            CreateDomainCustomer("cus_001", "C202604010001", "Alice Teller", "110101199001011234", "13800000001", CustomerStatus.Pending, firstCreatedAt),
            CreateDomainCustomer("cus_002", "C202604020001", "Bob Teller", "ID-789", "13800000002", CustomerStatus.Active, secondCreatedAt)
        };

        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        var service = CreateService(repository);

        var result = await service.GetAllAsync(2, 1, CancellationToken.None);

        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.CustomerId.Should().Be("cus_002");
        item.FullName.Should().Be("Bob Teller");
        item.IdentityNumberMasked.Should().Be("********");
        item.PortalIdentityLast4.Should().Be("0789");

        repository.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyPageMetadata_When_NoCustomersExist()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Domain.Customer>());

        var service = CreateService(repository);

        var result = await service.GetAllAsync(1, 20, CancellationToken.None);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.Items.Should().BeEmpty();
        repository.VerifyAll();
    }

    [Fact]
    public async Task ChangeStatus_Should_UpdateCustomer_When_TransitionIsAllowed()
    {
        var existing = CreateDomainCustomer(
            "cus_001",
            "C202604020001",
            "Alice Teller",
            "110101199001011234",
            "13800000001",
            CustomerStatus.Pending,
            new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));

        Domain.Customer? updatedCustomer = null;
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("cus_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(item => item.UpdateAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Customer, CancellationToken>((customer, _) => updatedCustomer = customer)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository);

        var result = await service.ChangeStatusAsync(
            "cus_001",
            new ChangeCustomerStatusRequest(CustomerStatus.Active, "Manual approval"),
            CancellationToken.None);

        result.Status.Should().Be(CustomerStatus.Active);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Status.Should().Be(CustomerStatus.Active);
        updatedCustomer.UpdatedAt.Should().BeAfter(existing.CreatedAt);

        repository.Verify(item => item.UpdateAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ChangeStatus_Should_Fail_When_TransitionIsInvalid()
    {
        var existing = CreateDomainCustomer(
            "cus_001",
            "C202604020001",
            "Alice Teller",
            "110101199001011234",
            "13800000001",
            CustomerStatus.Pending,
            new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));

        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("cus_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var service = CreateService(repository);

        var act = () => service.ChangeStatusAsync(
            "cus_001",
            new ChangeCustomerStatusRequest(CustomerStatus.Closed, "Invalid jump"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCustomerStatusTransitionException>();
        repository.Verify(item => item.UpdateAsync(It.IsAny<Domain.Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ChangeStatus_Should_Allow_SameStatusTransition()
    {
        var existing = CreateDomainCustomer(
            "cus_001",
            "C202604020001",
            "Alice Teller",
            "110101199001011234",
            "13800000001",
            CustomerStatus.Active,
            new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));

        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("cus_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(item => item.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository);

        var result = await service.ChangeStatusAsync(
            "cus_001",
            new ChangeCustomerStatusRequest(CustomerStatus.Active, "No-op update"),
            CancellationToken.None);

        result.Status.Should().Be(CustomerStatus.Active);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PortalSignIn_Should_Succeed_When_CustomerNumberAndIdentityLast4Match()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerNumberAsync("C202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainCustomer(
                "cus_001",
                "C202604020001",
                "Portal User",
                "WEB-1774756880023",
                "13800000009",
                CustomerStatus.Active,
                new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero)));

        var service = CreateService(repository);

        var signedIn = await service.SignInForPortalAsync(
            new CustomerPortalSignInRequest(" C202604020001 ", " 0023 "),
            CancellationToken.None);

        signedIn.CustomerId.Should().Be("cus_001");
        signedIn.IdentityNumberMasked.Should().EndWith("0023");
        repository.VerifyAll();
    }

    [Fact]
    public async Task PortalSignIn_Should_Fail_When_CustomerDoesNotExist()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerNumberAsync("C202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Customer?)null);

        var service = CreateService(repository);

        var act = () => service.SignInForPortalAsync(
            new CustomerPortalSignInRequest("C202604020001", "0023"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCustomerPortalSignInException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task PortalSignIn_Should_Fail_When_IdentityLast4LengthIsInvalid()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerNumberAsync("C202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainCustomer(
                "cus_001",
                "C202604020001",
                "Portal User",
                "WEB-1774756880023",
                "13800000009",
                CustomerStatus.Active,
                new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero)));

        var service = CreateService(repository);

        var act = () => service.SignInForPortalAsync(
            new CustomerPortalSignInRequest("C202604020001", "23"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCustomerPortalSignInException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task PortalSignIn_Should_Fail_When_IdentityLast4DoesNotMatch()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerNumberAsync("C202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainCustomer(
                "cus_001",
                "C202604020001",
                "Portal User",
                "WITHDRAW-DEMO-001",
                "13800000009",
                CustomerStatus.Active,
                new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero)));

        var service = CreateService(repository);

        var act = () => service.SignInForPortalAsync(
            new CustomerPortalSignInRequest("C202604020001", "9999"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCustomerPortalSignInException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task PortalSignIn_Should_Normalize_NonStandardIdentityNumbers_To_Last4Digits()
    {
        var repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerNumberAsync("C202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainCustomer(
                "cus_001",
                "C202604020001",
                "Portal User",
                "WITHDRAW-DEMO-001",
                "13800000009",
                CustomerStatus.Active,
                new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero)));

        var service = CreateService(repository);

        var result = await service.SignInForPortalAsync(
            new CustomerPortalSignInRequest("C202604020001", "0001"),
            CancellationToken.None);

        result.CustomerId.Should().Be("cus_001");
        repository.VerifyAll();
    }

    private static CustomerService CreateService(Mock<ICustomerRepository> repository)
        => new(repository.Object);

    private static CreateCustomerRequest CreateRequest() =>
        new(
            " Alice Teller ",
            " NationalId ",
            " 110101199001011234 ",
            " 13800000001 ",
            " alice@example.com ",
            new AddressRequest(" CN ", " Beijing ", " Beijing ", " No.1 Road ", " 100000 "),
            " Low ");

    private static Domain.Customer CreateDomainCustomer(
        string customerId,
        string customerNumber,
        string fullName,
        string identityNumber,
        string mobile,
        CustomerStatus status,
        DateTimeOffset createdAt) =>
        new()
        {
            CustomerId = customerId,
            CustomerNumber = customerNumber,
            FullName = fullName,
            IdentityType = "NationalId",
            IdentityNumber = identityNumber,
            Mobile = mobile,
            Email = $"{customerId}@example.com",
            Address = new Address("US", "New York", "New York", "1 Demo Plaza", "10001"),
            RiskLevel = "Low",
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
}
