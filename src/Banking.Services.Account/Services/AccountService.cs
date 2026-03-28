using Banking.BuildingBlocks.Contracts;
using Banking.Services.Account.Contracts;
using Banking.Services.Account.CustomerDirectory;
using Banking.Services.Account.Domain;
using Banking.Services.Account.Exceptions;
using Banking.Services.Account.Repositories;

namespace Banking.Services.Account.Services;

public sealed class AccountService(
    IAccountRepository accountRepository,
    ICustomerDirectory customerDirectory) : IAccountService
{
    public async Task<AccountResponse> OpenAsync(OpenAccountRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerDirectory.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new CustomerNotEligibleForAccountOpeningException(request.CustomerId, "Customer was not found.");
        }

        if (customer.Status != CustomerDirectoryStatus.Active)
        {
            throw new CustomerNotEligibleForAccountOpeningException(
                request.CustomerId,
                $"Customer status is '{customer.Status}'.");
        }

        var now = DateTimeOffset.UtcNow;
        var account = new Domain.Account
        {
            AccountId = $"acc_{Guid.NewGuid():N}",
            AccountNumber = $"6222{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
            CustomerId = request.CustomerId,
            AccountType = request.AccountType.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Status = AccountStatus.Active,
            AvailableBalance = 0m,
            LedgerBalance = 0m,
            OpenedAt = now
        };

        await accountRepository.AddAsync(account, cancellationToken);
        return Map(account);
    }

    public async Task<AccountResponse> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        return Map(account);
    }

    public async Task<PagedResponse<AccountSummaryResponse>> GetByCustomerIdAsync(
        string customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accounts = await accountRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var totalCount = accounts.Count;
        var items = accounts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(account => new AccountSummaryResponse(
                account.AccountId,
                account.AccountNumber,
                account.AccountType,
                account.Currency,
                account.Status,
                account.AvailableBalance,
                account.LedgerBalance))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<AccountSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    private static AccountResponse Map(Domain.Account account)
    {
        return new AccountResponse(
            account.AccountId,
            account.AccountNumber,
            account.CustomerId,
            account.AccountType,
            account.Currency,
            account.Status,
            account.AvailableBalance,
            account.LedgerBalance,
            account.OpenedAt,
            account.ClosedAt);
    }
}
