using Banking.BuildingBlocks.Contracts;
using Banking.Services.Audit.Contracts;
using Banking.Services.Audit.Domain;
using Banking.Services.Audit.Exceptions;
using Banking.Services.Audit.Repositories;

namespace Banking.Services.Audit.Services;

public sealed class AuditService(IAuditRepository auditRepository) : IAuditService
{
    public async Task<AuditLogResponse> RecordAsync(CreateAuditLogRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ActorId))
        {
            throw new InvalidAuditLogException("ActorId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            throw new InvalidAuditLogException("Action is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AggregateType) || string.IsNullOrWhiteSpace(request.AggregateId))
        {
            throw new InvalidAuditLogException("AggregateType and AggregateId are required.");
        }

        var auditLog = new AuditLog
        {
            AuditId = $"aud_{Guid.NewGuid():N}",
            ActorType = request.ActorType.Trim(),
            ActorId = request.ActorId.Trim(),
            Action = request.Action.Trim(),
            AggregateType = request.AggregateType.Trim(),
            AggregateId = request.AggregateId.Trim(),
            BeforeSnapshot = request.BeforeSnapshot,
            AfterSnapshot = request.AfterSnapshot,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? Guid.NewGuid().ToString("D")
                : request.CorrelationId.Trim(),
            CausationId = request.CausationId?.Trim(),
            OccurredAt = DateTimeOffset.UtcNow
        };

        await auditRepository.AddAsync(auditLog, cancellationToken);
        return Map(auditLog);
    }

    public async Task<AuditLogResponse> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        var auditLog = await auditRepository.GetByIdAsync(auditId, cancellationToken)
            ?? throw new AuditLogNotFoundException(auditId);

        return Map(auditLog);
    }

    public async Task<PagedResponse<AuditSummaryResponse>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var auditLogs = await auditRepository.GetAllAsync(cancellationToken);
        var totalCount = auditLogs.Count;
        var items = auditLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditSummaryResponse(
                item.AuditId,
                item.ActorType,
                item.ActorId,
                item.Action,
                item.AggregateType,
                item.AggregateId,
                item.CorrelationId,
                item.OccurredAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<AuditSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    private static AuditLogResponse Map(AuditLog auditLog)
    {
        return new AuditLogResponse(
            auditLog.AuditId,
            auditLog.ActorType,
            auditLog.ActorId,
            auditLog.Action,
            auditLog.AggregateType,
            auditLog.AggregateId,
            auditLog.BeforeSnapshot,
            auditLog.AfterSnapshot,
            auditLog.CorrelationId,
            auditLog.CausationId,
            auditLog.OccurredAt);
    }
}
