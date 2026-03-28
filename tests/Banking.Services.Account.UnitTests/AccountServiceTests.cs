using Banking.Services.Account.Contracts;
using Banking.Services.Account.CustomerDirectory;
using Banking.Services.Account.Exceptions;
using Banking.Services.Account.Repositories;
using Banking.Services.Account.Services;
using Banking.Services.Account.Domain;
using FluentAssertions;

namespace Banking.Services.Account.UnitTests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task OpenAccount_Should_Succeed_ForActiveCustomer()
    {
        var service = new AccountService(new InMemoryAccountRepository(), new InMemoryCustomerDirectory());

        var account = await service.OpenAsync(new OpenAccountRequest("cus_active_001", "Checking", "CNY"), CancellationToken.None);

        account.CustomerId.Should().Be("cus_active_001");
        account.Status.Should().Be(AccountStatus.Active);
        account.AvailableBalance.Should().Be(0m);
    }

    [Fact]
    public async Task OpenAccount_Should_Fail_ForFrozenCustomer()
    {
        var service = new AccountService(new InMemoryAccountRepository(), new InMemoryCustomerDirectory());

        var act = () => service.OpenAsync(new OpenAccountRequest("cus_frozen_001", "Checking", "CNY"), CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotEligibleForAccountOpeningException>();
    }
}
