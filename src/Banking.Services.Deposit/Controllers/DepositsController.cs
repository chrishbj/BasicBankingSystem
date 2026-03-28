using Banking.BuildingBlocks.Observability;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Deposit.Controllers;

[ApiController]
[Route("api/v1/deposits")]
public sealed class DepositsController(IDepositService depositService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepositRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Missing idempotency key",
                Detail = "Idempotency-Key header is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
            ?? Guid.NewGuid().ToString("D");

        try
        {
            var deposit = await depositService.CreateAsync(
                request,
                idempotencyKey,
                correlationId,
                cancellationToken);

            return AcceptedAtAction(nameof(GetById), new { transactionId = deposit.TransactionId }, deposit);
        }
        catch (InvalidDepositRequestException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid deposit request",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet("{transactionId}")]
    public async Task<IActionResult> GetById(string transactionId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await depositService.GetByIdAsync(transactionId, cancellationToken));
        }
        catch (DepositNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await depositService.GetAllAsync(pageNumber, pageSize, cancellationToken));
    }
}
