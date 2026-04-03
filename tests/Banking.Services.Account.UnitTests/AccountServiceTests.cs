using Banking.Services.Account.Contracts;
using Banking.Services.Account.CustomerDirectory;
using Banking.Services.Account.Domain;
using Banking.Services.Account.Exceptions;
using Banking.Services.Account.Repositories;
using Banking.Services.Account.Services;
using FluentAssertions;
using Moq;
using Banking.Services.Account.UnitTests.Support;

namespace Banking.Services.Account.UnitTests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task OpenAccount_Should_Succeed_ForActiveCustomer()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        Domain.Account? savedAccount = null;

        customerDirectory
            .Setup(item => item.GetByIdAsync("cus_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDirectoryRecord("cus_active_001", CustomerDirectoryStatus.Active));
        repository
            .Setup(item => item.AddAsync(It.IsAny<Domain.Account>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Account, CancellationToken>((account, _) => savedAccount = account)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, customerDirectory);

        var account = await service.OpenAsync(new OpenAccountRequest("cus_active_001", " Checking ", " usd "), CancellationToken.None);

        account.CustomerId.Should().Be("cus_active_001");
        account.Status.Should().Be(AccountStatus.Active);
        account.AvailableBalance.Should().Be(0m);
        savedAccount.Should().NotBeNull();
        savedAccount!.AccountType.Should().Be("Checking");
        savedAccount.Currency.Should().Be("USD");
        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Account>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
        customerDirectory.VerifyAll();
    }

    [Fact]
    public async Task OpenAccount_Should_Fail_ForFrozenCustomer()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);

        customerDirectory
            .Setup(item => item.GetByIdAsync("cus_frozen_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDirectoryRecord("cus_frozen_001", CustomerDirectoryStatus.Frozen));

        var service = CreateService(repository, customerDirectory);

        var act = () => service.OpenAsync(new OpenAccountRequest("cus_frozen_001", "Checking", "USD"), CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotEligibleForAccountOpeningException>();
        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Account>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        customerDirectory.VerifyAll();
    }

    [Fact]
    public async Task OpenAccount_Should_Fail_When_CustomerDoesNotExist()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);

        customerDirectory
            .Setup(item => item.GetByIdAsync("cus_missing_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDirectoryRecord?)null);

        var service = CreateService(repository, customerDirectory);

        var act = () => service.OpenAsync(new OpenAccountRequest("cus_missing_001", "Checking", "USD"), CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotEligibleForAccountOpeningException>();
        repository.Verify(item => item.AddAsync(It.IsAny<Domain.Account>(), It.IsAny<CancellationToken>()), Times.Never);
        customerDirectory.VerifyAll();
    }

    [Fact]
    public async Task ApplyDeposit_Should_UpdateBalances_ForActiveAccount()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = CreateAccount(balance: 0m);
        Domain.AccountPosting? savedPosting = null;

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .Setup(item => item.GetPostingByReferenceAsync("posting-unit-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.AccountPosting?)null);
        repository
            .Setup(item => item.SavePostingAsync(account, It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Account, Domain.AccountPosting, CancellationToken>((_, posting, _) => savedPosting = posting)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, customerDirectory);

        var updated = await service.ApplyDepositAsync(
            account.AccountId,
            new ApplyDepositRequest(250m, "usd", "posting-unit-001", "corr-unit-001"),
            CancellationToken.None);

        updated.AvailableBalance.Should().Be(250m);
        updated.LedgerBalance.Should().Be(250m);
        savedPosting.Should().NotBeNull();
        savedPosting!.PostingType.Should().Be(AccountPostingType.DepositCredit);
        savedPosting.Currency.Should().Be("USD");
        repository.Verify(item => item.SavePostingAsync(account, It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetByAccountNumber_Should_ReturnAccount_When_NumberExists()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = CreateAccount(accountId: "acc_001", accountNumber: "6222202604020001");

        repository
            .Setup(item => item.GetByAccountNumberAsync("6222202604020001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService(repository, customerDirectory);

        var fetched = await service.GetByAccountNumberAsync(" 6222202604020001 ", CancellationToken.None);

        fetched.AccountId.Should().Be("acc_001");
        fetched.AccountNumber.Should().Be("6222202604020001");
        repository.VerifyAll();
    }

    [Fact]
    public async Task Withdraw_Should_UpdateBalances_ForActiveAccount()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = CreateAccount(balance: 250m);
        Domain.AccountPosting? savedPosting = null;

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .Setup(item => item.GetPostingByReferenceAsync("withdrawal-unit-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.AccountPosting?)null);
        repository
            .Setup(item => item.SavePostingAsync(account, It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Account, Domain.AccountPosting, CancellationToken>((_, posting, _) => savedPosting = posting)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, customerDirectory);

        var updated = await service.WithdrawAsync(
            account.AccountId,
            new CreateWithdrawalRequest(100m, "USD", "withdrawal-unit-001", "corr-unit-002", "cash withdrawal"),
            CancellationToken.None);

        updated.AvailableBalance.Should().Be(150m);
        updated.LedgerBalance.Should().Be(150m);
        savedPosting.Should().NotBeNull();
        savedPosting!.PostingType.Should().Be(AccountPostingType.WithdrawalDebit);
        savedPosting.Amount.Should().Be(100m);
        repository.Verify(item => item.SavePostingAsync(account, It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ApplyDeposit_Should_ReturnCurrentState_When_PostingReferenceIsIdempotent()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = AccountServiceTestData.CreateAccount(balance: 250m);
        var existingPosting = AccountServiceTestData.CreatePosting("posting-unit-001", AccountPostingType.DepositCredit, 50m);

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .Setup(item => item.GetPostingByReferenceAsync("posting-unit-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPosting);

        var service = CreateService(repository, customerDirectory);

        var result = await service.ApplyDepositAsync(
            account.AccountId,
            new ApplyDepositRequest(50m, "USD", "posting-unit-001", "corr-unit-001"),
            CancellationToken.None);

        result.AvailableBalance.Should().Be(250m);
        repository.Verify(item => item.SavePostingAsync(It.IsAny<Domain.Account>(), It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ApplyDeposit_Should_Fail_When_AmountIsNotPositive()
    {
        var service = CreateService(new Mock<IAccountRepository>(MockBehavior.Strict), new Mock<ICustomerDirectory>(MockBehavior.Strict));

        var act = () => service.ApplyDepositAsync(
            "acc_active_001",
            new ApplyDepositRequest(0m, "USD", "posting-unit-001", "corr-unit-001"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotEligibleForDepositException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task ReverseDeposit_Should_SaveReversalPosting_When_RequestIsValid()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = AccountServiceTestData.CreateAccount(balance: 250m);
        var originalPosting = AccountServiceTestData.CreatePosting("orig-001", AccountPostingType.DepositCredit, 100m);
        Domain.AccountPosting? savedPosting = null;

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .SetupSequence(item => item.GetPostingByReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.AccountPosting?)null)
            .ReturnsAsync(originalPosting);
        repository
            .Setup(item => item.SavePostingAsync(account, It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Account, Domain.AccountPosting, CancellationToken>((_, posting, _) => savedPosting = posting)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, customerDirectory);

        var result = await service.ReverseDepositAsync(
            account.AccountId,
            new ReverseDepositRequest("rev-001", "orig-001", 100m, "usd", "corr-unit-003", "Compensate deposit"),
            CancellationToken.None);

        result.AvailableBalance.Should().Be(150m);
        savedPosting.Should().NotBeNull();
        savedPosting!.PostingType.Should().Be(AccountPostingType.DepositReversal);
        savedPosting.ReversalOfPostingReference.Should().Be("orig-001");
        repository.VerifyAll();
    }

    [Fact]
    public async Task Withdraw_Should_Fail_When_BalanceIsInsufficient()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = AccountServiceTestData.CreateAccount(balance: 50m);

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .Setup(item => item.GetPostingByReferenceAsync("withdrawal-unit-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.AccountPosting?)null);

        var service = CreateService(repository, customerDirectory);

        var act = () => service.WithdrawAsync(
            account.AccountId,
            new CreateWithdrawalRequest(100m, "USD", "withdrawal-unit-001", "corr-unit-002", "cash withdrawal"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotEligibleForWithdrawalException>()
            .WithMessage("*Insufficient balance*");
        repository.Verify(item => item.SavePostingAsync(It.IsAny<Domain.Account>(), It.IsAny<Domain.AccountPosting>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetByCustomerId_Should_ReturnPagedAccounts()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByCustomerIdAsync("cus_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                AccountServiceTestData.CreateAccount(accountId: "acc_001", accountNumber: "6222001", balance: 10m),
                AccountServiceTestData.CreateAccount(accountId: "acc_002", accountNumber: "6222002", balance: 20m)
            });

        var service = CreateService(repository, customerDirectory);

        var result = await service.GetByCustomerIdAsync("cus_active_001", 2, 1, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items.Single().AccountId.Should().Be("acc_002");
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetActivities_Should_FilterByType_And_DateRange_WithPaging()
    {
        var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
        var customerDirectory = new Mock<ICustomerDirectory>(MockBehavior.Strict);
        var account = AccountServiceTestData.CreateAccount();
        var baseTime = new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);

        repository
            .Setup(item => item.GetByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        repository
            .Setup(item => item.GetPostingsByAccountIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                AccountServiceTestData.CreatePosting("p1", AccountPostingType.DepositCredit, 100m),
                new Domain.AccountPosting
                {
                    PostingReference = "p2",
                    AccountId = account.AccountId,
                    PostingType = AccountPostingType.WithdrawalDebit,
                    Amount = 50m,
                    Currency = "USD",
                    CreatedAt = baseTime.AddMinutes(1)
                },
                new Domain.AccountPosting
                {
                    PostingReference = "p3",
                    AccountId = account.AccountId,
                    PostingType = AccountPostingType.WithdrawalDebit,
                    Amount = 25m,
                    Currency = "USD",
                    CreatedAt = baseTime.AddMinutes(2)
                }
            });

        var service = CreateService(repository, customerDirectory);

        var result = await service.GetActivitiesAsync(
            account.AccountId,
            1,
            1,
            "withdrawaldebit",
            baseTime,
            baseTime.AddMinutes(3),
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items.Single().PostingReference.Should().Be("p2");
        repository.VerifyAll();
    }

    private static AccountService CreateService(Mock<IAccountRepository> repository, Mock<ICustomerDirectory> customerDirectory)
        => new(repository.Object, customerDirectory.Object);

    private static Domain.Account CreateAccount(
        string accountId = "acc_active_001",
        string accountNumber = "6222202604029999",
        decimal balance = 0m)
        => AccountServiceTestData.CreateAccount(accountId, accountNumber, balance);
}
