using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Banking.Services.Account.Contracts;
using Banking.Services.Account.Exceptions;
using Banking.Services.Account.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Account.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize(Policy = BankingPolicies.ExternalOrInternal)]
public sealed class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Open([FromBody] OpenAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountService.OpenAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { accountId = account.AccountId }, account);
        }
        catch (CustomerNotEligibleForAccountOpeningException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Customer is not eligible for account opening",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetById(string accountId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountService.GetByIdAsync(accountId, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("by-number/{accountNumber}")]
    public async Task<IActionResult> GetByAccountNumber(string accountNumber, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountService.GetByAccountNumberAsync(accountNumber, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{accountId}/deposit-postings")]
    [Authorize(Policy = BankingPolicies.InternalServiceOnly)]
    public async Task<IActionResult> ApplyDeposit(
        string accountId,
        [FromBody] ApplyDepositRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountService.ApplyDepositAsync(accountId, request, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
        catch (AccountNotEligibleForDepositException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Account is not eligible for deposit",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("{accountId}/withdrawals")]
    public async Task<IActionResult> Withdraw(
        string accountId,
        [FromBody] CreateWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountService.WithdrawAsync(accountId, request, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
        catch (AccountNotEligibleForWithdrawalException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Account is not eligible for withdrawal",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("{accountId}/deposit-reversals")]
    [Authorize(Policy = BankingPolicies.InternalServiceOnly)]
    public async Task<IActionResult> ReverseDeposit(
        string accountId,
        [FromBody] ReverseDepositRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountService.ReverseDepositAsync(accountId, request, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
        catch (AccountDepositCompensationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Account deposit compensation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByCustomerId(
        [FromQuery] string customerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await accountService.GetByCustomerIdAsync(customerId, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet("{accountId}/activities")]
    public async Task<IActionResult> GetActivities(
        string accountId,
        [FromQuery] string? activityType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await accountService.GetActivitiesAsync(accountId, pageNumber, pageSize, activityType, from, to, cancellationToken));
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
    }
}
