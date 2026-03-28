using Banking.BuildingBlocks.Contracts;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;

namespace Banking.Services.Deposit.Services;

public sealed class DepositService(
    IDepositRepository depositRepository,
    IDepositAccountDirectory accountDirectory) : IDepositService
{
    public async Task<DepositResponse> CreateAsync(
        CreateDepositRequest request,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidDepositRequestException("Deposit amount must be greater than zero.");
        }

        var existing = await depositRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var account = await accountDirectory.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new InvalidDepositRequestException("Account was not found.");
        }

        if (!string.Equals(account.CustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            throw new InvalidDepositRequestException("Customer and account do not match.");
        }

        if (account.Status != DepositAccountStatus.Active)
        {
            throw new InvalidDepositRequestException($"Account status is '{account.Status}'.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDepositRequestException("Currency does not match account currency.");
        }

        var now = DateTimeOffset.UtcNow;
        var transaction = new DepositTransaction
        {
            TransactionId = $"dep_{Guid.NewGuid():N}",
            TransactionNumber = $"D{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Channel = request.Channel,
            Status = DepositStatus.Received,
            AccountPostingStatus = DepositSagaStepStatus.NotStarted,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.NotStarted,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            RequestedAt = now
        };

        var requestedMessage = new DepositRequestedMessage(
            transaction.TransactionId,
            transaction.CustomerId,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.Channel,
            transaction.CorrelationId);

        await depositRepository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(requestedMessage, now),
            cancellationToken);

        return Map(transaction);
    }

    public async Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        var transaction = await depositRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new DepositNotFoundException(transactionId);

        return Map(transaction);
    }

    public async Task<PagedResponse<DepositSummaryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var deposits = await depositRepository.GetAllAsync(cancellationToken);
        var totalCount = deposits.Count;
        var items = deposits
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new DepositSummaryResponse(
                item.TransactionId,
                item.TransactionNumber,
                item.CustomerId,
                item.AccountId,
                item.Amount,
                item.Currency,
                item.Channel,
                item.Status,
                item.RequestedAt,
                item.PostedAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<DepositSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    private static DepositResponse Map(DepositTransaction transaction)
    {
        return new DepositResponse(
            transaction.TransactionId,
            transaction.TransactionNumber,
            transaction.CustomerId,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.Channel,
            transaction.Status,
            transaction.AccountPostingStatus,
            transaction.AuditStatus,
            transaction.CompensationStatus,
            transaction.CorrelationId,
            transaction.FailureCode,
            transaction.FailureReason,
            transaction.RequestedAt,
            transaction.PostedAt,
            transaction.ReversedAt,
            transaction.LastProcessedAt);
    }
}
