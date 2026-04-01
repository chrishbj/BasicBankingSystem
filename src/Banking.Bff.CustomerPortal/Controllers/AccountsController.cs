using Banking.Bff.CustomerPortal.Auth;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Bff.CustomerPortal.Controllers;

[ApiController]
[Route("api/customer-portal/accounts")]
[Authorize]
public sealed class AccountsController(
    AccountServiceClient accountServiceClient,
    ICurrentPortalCustomerAccessor currentPortalCustomerAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var response = await accountServiceClient.GetByCustomerIdAsync(customerId, pageNumber, pageSize, cancellationToken);
            return Ok(new
            {
                Items = response.Items.Select(MapAccountSummary).ToArray(),
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

    [HttpGet("{accountNumber}")]
    public async Task<IActionResult> GetById(string accountNumber, CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var account = await accountServiceClient.GetByAccountNumberAsync(accountNumber, cancellationToken);
            if (!string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            return Ok(MapAccount(account));
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

    [HttpGet("{accountNumber}/activities")]
    public async Task<IActionResult> GetActivities(
        string accountNumber,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var account = await accountServiceClient.GetByAccountNumberAsync(accountNumber, cancellationToken);
            if (!string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var query = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            var response = await accountServiceClient.GetActivitiesAsync(account.AccountId, query, cancellationToken);
            return Ok(new
            {
                Items = response.Items.Select(MapActivity).ToArray(),
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

    [HttpPost("{accountNumber}/withdrawals")]
    public async Task<IActionResult> Withdraw(
        string accountNumber,
        [FromBody] CreateWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var account = await accountServiceClient.GetByAccountNumberAsync(accountNumber, cancellationToken);
            if (!string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var updated = await accountServiceClient.WithdrawAsync(account.AccountId, request, cancellationToken);
            return Ok(MapAccount(updated));
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

    private static object MapAccount(AccountResponse account) => new
    {
        account.AccountNumber,
        account.AccountType,
        account.Currency,
        account.Status,
        account.AvailableBalance,
        account.LedgerBalance,
        account.OpenedAt,
        account.ClosedAt
    };

    private static object MapAccountSummary(AccountSummaryResponse account) => new
    {
        account.AccountNumber,
        account.AccountType,
        account.Currency,
        account.Status,
        account.AvailableBalance,
        account.LedgerBalance
    };

    private static object MapActivity(AccountActivityResponse activity) => new
    {
        activity.PostingReference,
        activity.PostingType,
        activity.Amount,
        activity.Currency,
        activity.CorrelationId,
        activity.ReversalOfPostingReference,
        activity.CreatedAt
    };
}
