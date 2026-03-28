using Banking.BuildingBlocks.Contracts;
using Banking.Services.Audit.Contracts;

namespace Banking.Services.Audit.Services;

public interface IAuditService
{
    Task<AuditLogResponse> RecordAsync(CreateAuditLogRequest request, CancellationToken cancellationToken);
    Task<AuditLogResponse> GetByIdAsync(string auditId, CancellationToken cancellationToken);
    Task<PagedResponse<AuditSummaryResponse>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
