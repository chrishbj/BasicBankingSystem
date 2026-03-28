using Banking.BuildingBlocks.Contracts;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Services;

public interface IDepositService
{
    Task<DepositResponse> CreateAsync(CreateDepositRequest request, string idempotencyKey, string correlationId, CancellationToken cancellationToken);
    Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken);
    Task<PagedResponse<DepositSummaryResponse>> GetAllAsync(DepositStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<PendingReviewDepositSummaryResponse>> GetPendingReviewAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<DepositResponse> RetryPendingReviewAsync(string transactionId, RetryDepositReviewRequest request, CancellationToken cancellationToken);
    Task<DepositResponse> ResolvePendingReviewAsync(string transactionId, ResolveDepositReviewRequest request, CancellationToken cancellationToken);
}
