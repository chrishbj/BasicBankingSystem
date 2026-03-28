using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositOutboxDispatcherTests
{
    [Fact]
    public async Task DispatchPendingMessagesAsync_Should_PublishAndMarkProcessed()
    {
        var repository = new InMemoryDepositRepository();
        var message = new DepositRequestedMessage("dep_001", "cus_active_001", "acc_active_001", 100m, "CNY", Domain.DepositChannel.Counter, "corr-001");

        await repository.AddAsync(
            new Domain.DepositTransaction
            {
                TransactionId = "dep_001",
                TransactionNumber = "D001",
                CustomerId = "cus_active_001",
                AccountId = "acc_active_001",
                Amount = 100m,
                Currency = "CNY",
                Channel = Domain.DepositChannel.Counter,
                Status = Domain.DepositStatus.Received,
                IdempotencyKey = "idem-001",
                CorrelationId = "corr-001",
                RequestedAt = DateTimeOffset.UtcNow
            },
            DepositOutboxMessage.CreateRequestedMessage(message, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var publisher = new TestDepositEventPublisher();
        var services = new ServiceCollection();
        services.AddSingleton<IDepositRepository>(repository);
        using var provider = services.BuildServiceProvider();

        var dispatcher = new DepositOutboxDispatcher(
            new StaticServiceScopeFactory(provider),
            publisher,
            Options.Create(new RabbitMqOptions { PollingIntervalMilliseconds = 50 }),
            NullLogger<DepositOutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingMessagesAsync(CancellationToken.None);

        publisher.Messages.Should().ContainSingle();
        var pendingMessages = await repository.GetPendingOutboxMessagesAsync(10, CancellationToken.None);
        pendingMessages.Should().BeEmpty();
    }

    private sealed class TestDepositEventPublisher : IDepositEventPublisher
    {
        public List<DepositRequestedMessage> Messages { get; } = new();

        public Task PublishAsync(DepositRequestedMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class StaticServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new StaticServiceScope(serviceProvider);
        }
    }

    private sealed class StaticServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
        }
    }
}
