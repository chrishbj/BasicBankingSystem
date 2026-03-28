using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositServiceTests
{
    private readonly InMemoryDepositRepository _repository;
    private readonly IDepositService _service;

    public DepositServiceTests()
    {
        _repository = new InMemoryDepositRepository();
        _service = new DepositService(_repository, new InMemoryDepositAccountDirectory());
    }

    [Fact]
    public async Task CreateDeposit_Should_Fail_When_AmountIsLessThanOrEqualToZero()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 0m, "CNY", DepositChannel.Counter, null, null);

        var act = () => _service.CreateAsync(request, "idem-001", "corr-001", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositRequestException>();
    }

    [Fact]
    public async Task CreateDeposit_Should_CreateReceivedTransaction_When_RequestIsValid()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 200m, "CNY", DepositChannel.Counter, null, null);

        var result = await _service.CreateAsync(request, "idem-002", "corr-002", CancellationToken.None);

        result.Status.Should().Be(DepositStatus.Received);
        result.PostedAt.Should().BeNull();

        var stored = await _repository.GetByIdAsync(result.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Received);
        var pendingMessages = await _repository.GetPendingOutboxMessagesAsync(10, CancellationToken.None);
        pendingMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateDeposit_Should_ReturnExistingResult_When_IdempotencyKeyRepeated()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 300m, "CNY", DepositChannel.Counter, null, null);

        var first = await _service.CreateAsync(request, "idem-003", "corr-003", CancellationToken.None);
        var second = await _service.CreateAsync(request, "idem-003", "corr-004", CancellationToken.None);

        second.TransactionId.Should().Be(first.TransactionId);
        second.TransactionNumber.Should().Be(first.TransactionNumber);
    }
}
