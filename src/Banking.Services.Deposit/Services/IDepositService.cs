using Banking.BuildingBlocks.Contracts;
using Banking.Services.Deposit.Contracts;

namespace Banking.Services.Deposit.Services;

public interface IDepositService
{
    Task<DepositResponse> CreateAsync(CreateDepositRequest request, string idempotencyKey, string correlationId, CancellationToken cancellationToken);
    Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken);
    Task<PagedResponse<DepositSummaryResponse>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
