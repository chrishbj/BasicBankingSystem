using System.Text.Json;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Banking.Services.Deposit.UnitTests.Support;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositOutboxDispatcherTests
{
    [Fact]
    public async Task DispatchPendingMessagesAsync_Should_PublishAndMarkProcessed()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var publisher = new Mock<IDepositEventPublisher>(MockBehavior.Strict);
        var message = new DepositRequestedMessage("dep_001", "cus_active_001", "acc_active_001", 100m, "USD", Domain.DepositChannel.Counter, "corr-001");
        var outbox = new DepositOutboxMessage
        {
            MessageId = "out_001",
            TransactionId = "dep_001",
            MessageType = nameof(DepositRequestedMessage),
            Payload = JsonSerializer.Serialize(message),
            OccurredAt = DateTimeOffset.UtcNow
        };

        repository
            .Setup(item => item.GetPendingOutboxMessagesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { outbox });
        publisher
            .Setup(item => item.PublishAsync(
                It.Is<DepositRequestedMessage>(payload =>
                    payload.TransactionId == "dep_001" &&
                    payload.AccountId == "acc_active_001"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.MarkOutboxMessageProcessedAsync("out_001", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var provider = new ServiceCollection()
            .AddSingleton(repository.Object)
            .BuildServiceProvider();

        var dispatcher = new DepositOutboxDispatcher(
            new StaticServiceScopeFactory(provider),
            publisher.Object,
            Options.Create(new RabbitMqOptions { PollingIntervalMilliseconds = 50 }),
            NullLogger<DepositOutboxDispatcher>.Instance);

        var count = await dispatcher.DispatchPendingMessagesAsync(CancellationToken.None);

        count.Should().Be(1);
        repository.VerifyAll();
        publisher.VerifyAll();
    }

    [Fact]
    public async Task DispatchPendingMessagesAsync_Should_MarkNullPayloadAsProcessed_WithoutPublishing()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var publisher = new Mock<IDepositEventPublisher>(MockBehavior.Strict);
        var outbox = new DepositOutboxMessage
        {
            MessageId = "out_invalid",
            TransactionId = "dep_invalid",
            MessageType = nameof(DepositRequestedMessage),
            Payload = "null",
            OccurredAt = DateTimeOffset.UtcNow
        };

        repository
            .Setup(item => item.GetPendingOutboxMessagesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { outbox });
        repository
            .Setup(item => item.MarkOutboxMessageProcessedAsync("out_invalid", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var provider = new ServiceCollection()
            .AddSingleton(repository.Object)
            .BuildServiceProvider();

        var dispatcher = new DepositOutboxDispatcher(
            new StaticServiceScopeFactory(provider),
            publisher.Object,
            Options.Create(new RabbitMqOptions { PollingIntervalMilliseconds = 50 }),
            NullLogger<DepositOutboxDispatcher>.Instance);

        var count = await dispatcher.DispatchPendingMessagesAsync(CancellationToken.None);

        count.Should().Be(1);
        publisher.Verify(item => item.PublishAsync(It.IsAny<DepositRequestedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    private sealed class StaticServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new StaticServiceScope(serviceProvider);
    }

    private sealed class StaticServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
        }
    }
}
