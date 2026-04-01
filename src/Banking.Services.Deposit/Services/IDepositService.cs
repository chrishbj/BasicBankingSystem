using Banking.BuildingBlocks.Contracts;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Services;

public interface IDepositService
{
    Task<DepositResponse> CreateAsync(CreateDepositRequest request, string idempotencyKey, string correlationId, CancellationToken cancellationToken);
    Task<DepositResponse> CreatePendingReviewDemoAsync(CreatePendingReviewDemoRequest request, string correlationId, CancellationToken cancellationToken);
    Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken);
    Task<PagedResponse<DepositSummaryResponse>> GetAllAsync(DepositSearchRequest request, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<PendingReviewDepositSummaryResponse>> GetPendingReviewAsync(PendingReviewSortBy sortBy, bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<DepositRuntimeStatusResponse> GetRuntimeStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepositOutboxMessageResponse>> GetOutboxMessagesAsync(int maxCount, bool pendingOnly, CancellationToken cancellationToken);
    Task<DepositOutboxMessageResponse?> GetOutboxMessageByIdAsync(string messageId, CancellationToken cancellationToken);
    Task<DepositOutboxMessageResponse?> RequeueOutboxMessageAsync(string messageId, RequeueDepositOutboxMessageRequest request, CancellationToken cancellationToken);
    Task<DepositResponse> RetryPendingReviewAsync(string transactionId, RetryDepositReviewRequest request, CancellationToken cancellationToken);
    Task<DepositResponse> ResolvePendingReviewAsync(string transactionId, ResolveDepositReviewRequest request, CancellationToken cancellationToken);
}
