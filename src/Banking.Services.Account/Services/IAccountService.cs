using Banking.BuildingBlocks.Contracts;
using Banking.Services.Account.Contracts;

namespace Banking.Services.Account.Services;

public interface IAccountService
{
    Task<AccountResponse> OpenAsync(OpenAccountRequest request, CancellationToken cancellationToken);
    Task<AccountResponse> GetByIdAsync(string accountId, CancellationToken cancellationToken);
    Task<AccountResponse> ApplyDepositAsync(string accountId, ApplyDepositRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<AccountSummaryResponse>> GetByCustomerIdAsync(
        string customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
