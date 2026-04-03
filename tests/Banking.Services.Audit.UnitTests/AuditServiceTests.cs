using Banking.Services.Audit.Contracts;
using Banking.Services.Audit.Domain;
using Banking.Services.Audit.Exceptions;
using Banking.Services.Audit.Repositories;
using Banking.Services.Audit.Services;
using FluentAssertions;
using Moq;
using Banking.Services.Audit.UnitTests.Support;

namespace Banking.Services.Audit.UnitTests;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task RecordAudit_Should_Succeed_When_RequestIsValid()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        AuditLog? savedAudit = null;

        repository
            .Setup(item => item.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((auditLog, _) => savedAudit = auditLog)
            .Returns(Task.CompletedTask);

        var service = new AuditService(repository.Object);
        var request = new CreateAuditLogRequest(
            " User ",
            " user_001 ",
            " CustomerCreated ",
            " Customer ",
            " cus_001 ",
            null,
            new Dictionary<string, object?> { ["status"] = "Pending" },
            " corr-001 ",
            " cmd-001 ");

        var result = await service.RecordAsync(request, CancellationToken.None);

        result.AuditId.Should().StartWith("aud_");
        result.Action.Should().Be("CustomerCreated");
        result.CorrelationId.Should().Be("corr-001");
        savedAudit.Should().NotBeNull();
        savedAudit!.ActorType.Should().Be("User");
        savedAudit.ActorId.Should().Be("user_001");
        savedAudit.AggregateType.Should().Be("Customer");
        savedAudit.AggregateId.Should().Be("cus_001");
        savedAudit.CausationId.Should().Be("cmd-001");
        repository.VerifyAll();
    }

    [Fact]
    public async Task RecordAudit_Should_Fail_When_ActionIsMissing()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        var service = new AuditService(repository.Object);
        var request = new CreateAuditLogRequest(
            "User",
            "user_001",
            "",
            "Customer",
            "cus_001",
            null,
            null,
            "corr-001",
            null);

        var act = () => service.RecordAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidAuditLogException>();
        repository.Verify(item => item.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAudit_Should_Fail_When_ActorIdIsMissing()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        var service = new AuditService(repository.Object);

        var act = () => service.RecordAsync(
            new CreateAuditLogRequest("User", " ", "CustomerCreated", "Customer", "cus_001", null, null, "corr-001", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidAuditLogException>()
            .WithMessage("*ActorId is required*");
    }

    [Fact]
    public async Task GetAll_Should_ReturnPagedAuditSummaries()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                AuditServiceTestData.CreateAuditLog("aud_001", "CustomerCreated", new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero)),
                AuditServiceTestData.CreateAuditLog("aud_002", "CustomerActivated", new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero))
            });

        var service = new AuditService(repository.Object);

        var result = await service.GetAllAsync(2, 1, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items.Single().AuditId.Should().Be("aud_002");
        repository.VerifyAll();
    }

    [Fact]
    public async Task RecordAudit_Should_GenerateCorrelationId_When_Missing()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        AuditLog? savedAudit = null;

        repository
            .Setup(item => item.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((auditLog, _) => savedAudit = auditLog)
            .Returns(Task.CompletedTask);

        var service = new AuditService(repository.Object);

        var result = await service.RecordAsync(
            new CreateAuditLogRequest("User", "user_001", "CustomerCreated", "Customer", "cus_001", null, null, null, null),
            CancellationToken.None);

        result.CorrelationId.Should().NotBeNullOrWhiteSpace();
        savedAudit!.CorrelationId.Should().Be(result.CorrelationId);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetById_Should_Throw_When_AuditLogDoesNotExist()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("aud_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);

        var service = new AuditService(repository.Object);

        var act = () => service.GetByIdAsync("aud_missing", CancellationToken.None);

        await act.Should().ThrowAsync<AuditLogNotFoundException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyPageMetadata_When_NoAuditLogsExist()
    {
        var repository = new Mock<IAuditRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AuditLog>());

        var service = new AuditService(repository.Object);

        var result = await service.GetAllAsync(1, 20, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.Items.Should().BeEmpty();
        repository.VerifyAll();
    }
}
