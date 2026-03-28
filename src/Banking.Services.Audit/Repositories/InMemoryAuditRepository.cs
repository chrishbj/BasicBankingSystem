using System.Collections.Concurrent;
using Banking.Services.Audit.Domain;

namespace Banking.Services.Audit.Repositories;

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly ConcurrentDictionary<string, AuditLog> _auditLogs = new();

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        _auditLogs[auditLog.AuditId] = auditLog;
        return Task.CompletedTask;
    }

    public Task<AuditLog?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        _auditLogs.TryGetValue(auditId, out var auditLog);
        return Task.FromResult(auditLog);
    }

    public Task<IReadOnlyCollection<AuditLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<AuditLog>>(
            _auditLogs.Values
                .OrderByDescending(item => item.OccurredAt)
                .ToArray());
    }
}
