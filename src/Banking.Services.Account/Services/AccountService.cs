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

    public async Task<AccountResponse> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var trimmedNumber = accountNumber.Trim();
        var account = await accountRepository.GetByAccountNumberAsync(trimmedNumber, cancellationToken)
            ?? throw new AccountNotFoundException(trimmedNumber);

        return Map(account);
    }

    public async Task<AccountResponse> ApplyDepositAsync(string accountId, ApplyDepositRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new AccountNotEligibleForDepositException(accountId, "Deposit amount must be greater than zero.");
        }

        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        if (account.Status != AccountStatus.Active)
        {
            throw new AccountNotEligibleForDepositException(accountId, $"Account status is '{account.Status}'.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new AccountNotEligibleForDepositException(accountId, "Currency does not match account currency.");
        }

        var existingPosting = await accountRepository.GetPostingByReferenceAsync(request.PostingReference, cancellationToken);
        if (existingPosting is not null)
        {
            if (existingPosting.AccountId != accountId ||
                existingPosting.PostingType != AccountPostingType.DepositCredit ||
                existingPosting.Amount != request.Amount ||
                !string.Equals(existingPosting.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new AccountNotEligibleForDepositException(accountId, "Posting reference was already used with different values.");
            }

            return Map(account);
        }

        account.AvailableBalance += request.Amount;
        account.LedgerBalance += request.Amount;

        await accountRepository.SavePostingAsync(
            account,
            new AccountPosting
            {
                PostingReference = request.PostingReference,
                AccountId = accountId,
                PostingType = AccountPostingType.DepositCredit,
                Amount = request.Amount,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                CorrelationId = request.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        return Map(account);
    }

    public async Task<AccountResponse> WithdrawAsync(string accountId, CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new AccountNotEligibleForWithdrawalException(accountId, "Withdrawal amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.ReferenceNumber))
        {
            throw new AccountNotEligibleForWithdrawalException(accountId, "Reference number is required.");
        }

        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        if (account.Status != AccountStatus.Active)
        {
            throw new AccountNotEligibleForWithdrawalException(accountId, $"Account status is '{account.Status}'.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new AccountNotEligibleForWithdrawalException(accountId, "Currency does not match account currency.");
        }

        var postingReference = request.ReferenceNumber.Trim();
        var existingPosting = await accountRepository.GetPostingByReferenceAsync(postingReference, cancellationToken);
        if (existingPosting is not null)
        {
            if (existingPosting.AccountId != accountId ||
                existingPosting.PostingType != AccountPostingType.WithdrawalDebit ||
                existingPosting.Amount != request.Amount ||
                !string.Equals(existingPosting.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new AccountNotEligibleForWithdrawalException(accountId, "Reference number was already used with different values.");
            }

            return Map(account);
        }

        if (account.AvailableBalance < request.Amount || account.LedgerBalance < request.Amount)
        {
            throw new AccountNotEligibleForWithdrawalException(accountId, "Insufficient balance.");
        }

        account.AvailableBalance -= request.Amount;
        account.LedgerBalance -= request.Amount;

        await accountRepository.SavePostingAsync(
            account,
            new AccountPosting
            {
                PostingReference = postingReference,
                AccountId = accountId,
                PostingType = AccountPostingType.WithdrawalDebit,
                Amount = request.Amount,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                CorrelationId = request.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        return Map(account);
    }

    public async Task<AccountResponse> ReverseDepositAsync(string accountId, ReverseDepositRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new AccountDepositCompensationException(accountId, "Reversal amount must be greater than zero.");
        }

        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        var existingReversal = await accountRepository.GetPostingByReferenceAsync(request.ReversalReference, cancellationToken);
        if (existingReversal is not null)
        {
            if (existingReversal.AccountId != accountId ||
                existingReversal.PostingType != AccountPostingType.DepositReversal ||
                existingReversal.Amount != request.Amount ||
                !string.Equals(existingReversal.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new AccountDepositCompensationException(accountId, "Reversal reference was already used with different values.");
            }

            return Map(account);
        }

        var originalPosting = await accountRepository.GetPostingByReferenceAsync(request.OriginalPostingReference, cancellationToken);
        if (originalPosting is null)
        {
            throw new AccountDepositCompensationException(accountId, "Original deposit posting was not found.");
        }

        if (originalPosting.AccountId != accountId || originalPosting.PostingType != AccountPostingType.DepositCredit)
        {
            throw new AccountDepositCompensationException(accountId, "Original posting does not belong to this account.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new AccountDepositCompensationException(accountId, "Currency does not match account currency.");
        }

        if (account.AvailableBalance < request.Amount || account.LedgerBalance < request.Amount)
        {
            throw new AccountDepositCompensationException(accountId, "Insufficient balance to apply compensation.");
        }

        account.AvailableBalance -= request.Amount;
        account.LedgerBalance -= request.Amount;

        await accountRepository.SavePostingAsync(
            account,
            new AccountPosting
            {
                PostingReference = request.ReversalReference,
                AccountId = accountId,
                PostingType = AccountPostingType.DepositReversal,
                Amount = request.Amount,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                CorrelationId = request.CorrelationId,
                ReversalOfPostingReference = request.OriginalPostingReference,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

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

    public async Task<PagedResponse<AccountActivityResponse>> GetActivitiesAsync(
        string accountId,
        int pageNumber,
        int pageSize,
        string? activityType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        var activities = await accountRepository.GetPostingsByAccountIdAsync(account.AccountId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(activityType) &&
            Enum.TryParse<AccountPostingType>(activityType.Trim(), true, out var parsedType))
        {
            activities = activities.Where(item => item.PostingType == parsedType).ToArray();
        }

        if (from is not null)
        {
            activities = activities.Where(item => item.CreatedAt >= from.Value).ToArray();
        }

        if (to is not null)
        {
            activities = activities.Where(item => item.CreatedAt <= to.Value).ToArray();
        }

        var totalCount = activities.Count;
        var items = activities
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AccountActivityResponse(
                item.PostingReference,
                item.AccountId,
                item.PostingType,
                item.Amount,
                item.Currency,
                item.CorrelationId,
                item.ReversalOfPostingReference,
                item.CreatedAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<AccountActivityResponse>(items, pageNumber, pageSize, totalCount, totalPages);
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
