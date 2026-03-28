using Banking.Services.Audit.Contracts;
using Banking.Services.Audit.Exceptions;
using Banking.Services.Audit.Repositories;
using Banking.Services.Audit.Services;
using FluentAssertions;

namespace Banking.Services.Audit.UnitTests;

public sealed class AuditServiceTests
{
    private readonly IAuditService _service;

    public AuditServiceTests()
    {
        _service = new AuditService(new InMemoryAuditRepository());
    }

    [Fact]
    public async Task RecordAudit_Should_Succeed_When_RequestIsValid()
    {
        var request = new CreateAuditLogRequest(
            "User",
            "user_001",
            "CustomerCreated",
            "Customer",
            "cus_001",
            null,
            new Dictionary<string, object?> { ["status"] = "Pending" },
            "corr-001",
            "cmd-001");

        var result = await _service.RecordAsync(request, CancellationToken.None);

        result.AuditId.Should().NotBeNullOrWhiteSpace();
        result.Action.Should().Be("CustomerCreated");
        result.CorrelationId.Should().Be("corr-001");
    }

    [Fact]
    public async Task RecordAudit_Should_Fail_When_ActionIsMissing()
    {
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

        var act = () => _service.RecordAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidAuditLogException>();
    }
}
