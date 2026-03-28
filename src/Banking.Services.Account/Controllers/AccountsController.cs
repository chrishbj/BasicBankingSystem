using Banking.Services.Account.Contracts;
using Banking.Services.Account.Exceptions;
using Banking.Services.Account.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Account.Controllers;

[ApiController]
[Route("api/v1/accounts")]
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

    [HttpPost("{accountId}/deposit-postings")]
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

    [HttpGet]
    public async Task<IActionResult> GetByCustomerId(
        [FromQuery] string customerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await accountService.GetByCustomerIdAsync(customerId, pageNumber, pageSize, cancellationToken));
    }
}
