using Banking.Bff.CustomerPortal.Auth;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Bff.CustomerPortal.Controllers;

[ApiController]
[Route("api/customer-portal/deposits")]
[Authorize]
public sealed class DepositsController(
    DepositServiceClient depositServiceClient,
    AccountServiceClient accountServiceClient,
    ICurrentPortalCustomerAccessor currentPortalCustomerAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePortalDepositRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var account = await accountServiceClient.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);
            if (!string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var idempotencyKey = $"bff-idem-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var correlationId = $"bff-corr-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var created = await depositServiceClient.CreateAsync(
                customerId,
                account.AccountId,
                request,
                idempotencyKey,
                correlationId,
                cancellationToken);

            return Accepted(MapDeposit(created, account.AccountNumber));
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    [HttpGet("{transactionNumber}")]
    public async Task<IActionResult> GetById(string transactionNumber, CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var accountLookup = await accountServiceClient.GetByCustomerIdAsync(customerId, 1, 200, cancellationToken);
            var accountNumbersById = accountLookup.Items.ToDictionary(item => item.AccountId, item => item.AccountNumber, StringComparer.Ordinal);
            var searchResult = await depositServiceClient.SearchAsync(customerId, null, 1, 200, cancellationToken);
            var summary = searchResult.Items.FirstOrDefault(item => string.Equals(item.TransactionNumber, transactionNumber, StringComparison.Ordinal));
            if (summary is null)
            {
                return NotFound();
            }

            var deposit = await depositServiceClient.GetByIdAsync(summary.TransactionId, cancellationToken);
            if (!string.Equals(deposit.CustomerId, customerId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            return Ok(MapDeposit(deposit, accountNumbersById.GetValueOrDefault(deposit.AccountId, "Unknown")));
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? accountNumber = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            string? accountId = null;
            string? selectedAccountNumber = null;

            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                var account = await accountServiceClient.GetByAccountNumberAsync(accountNumber, cancellationToken);
                if (!string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                accountId = account.AccountId;
                selectedAccountNumber = account.AccountNumber;
            }

            var response = await depositServiceClient.SearchAsync(customerId, accountId, pageNumber, pageSize, cancellationToken);
            Dictionary<string, string>? accountNumbersById = null;
            if (selectedAccountNumber is null)
            {
                var accounts = await accountServiceClient.GetByCustomerIdAsync(customerId, 1, 200, cancellationToken);
                accountNumbersById = accounts.Items.ToDictionary(item => item.AccountId, item => item.AccountNumber, StringComparer.Ordinal);
            }

            return Ok(new
            {
                Items = response.Items.Select(item => MapDepositSummary(item, selectedAccountNumber ?? accountNumbersById?.GetValueOrDefault(item.AccountId, "Unknown") ?? "Unknown")).ToArray(),
                response.PageNumber,
                response.PageSize,
                response.TotalCount,
                response.TotalPages
            });
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    private static object MapDeposit(DepositResponse deposit, string accountNumber) => new
    {
        deposit.TransactionNumber,
        AccountNumber = accountNumber,
        deposit.Amount,
        deposit.Currency,
        deposit.ReferenceNumber,
        deposit.Status,
        deposit.CorrelationId,
        deposit.FailureCode,
        deposit.FailureReason,
        deposit.RequestedAt,
        deposit.PostedAt
    };

    private static object MapDepositSummary(DepositSummaryResponse deposit, string accountNumber) => new
    {
        deposit.TransactionNumber,
        AccountNumber = accountNumber,
        deposit.Amount,
        deposit.Currency,
        deposit.ReferenceNumber,
        deposit.Status,
        deposit.RequestedAt,
        deposit.PostedAt
    };
}
