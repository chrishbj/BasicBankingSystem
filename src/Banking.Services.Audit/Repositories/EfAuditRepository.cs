using Banking.Services.Audit.Data;
using Banking.Services.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Audit.Repositories;

public sealed class EfAuditRepository(AuditDbContext dbContext) : IAuditRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AuditLog?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        return dbContext.AuditLogs.FirstOrDefaultAsync(x => x.AuditId == auditId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.AuditLogs.ToListAsync(cancellationToken);
        return items
            .OrderByDescending(item => item.OccurredAt)
            .ToArray();
    }
}
