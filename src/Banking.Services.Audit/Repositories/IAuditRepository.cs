using Banking.Services.Audit.Domain;

namespace Banking.Services.Audit.Repositories;

public interface IAuditRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task<AuditLog?> GetByIdAsync(string auditId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLog>> GetAllAsync(CancellationToken cancellationToken);
}
