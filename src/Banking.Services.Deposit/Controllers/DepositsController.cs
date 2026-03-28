using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Banking.BuildingBlocks.Observability;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Deposit.Controllers;

[ApiController]
[Route("api/v1/deposits")]
[Authorize(Policy = BankingPolicies.ExternalOrInternal)]
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
        [FromQuery] DepositStatus? status = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? failureCode = null,
        [FromQuery] DateTimeOffset? requestedFrom = null,
        [FromQuery] DateTimeOffset? requestedTo = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await depositService.GetAllAsync(
            new DepositSearchRequest(status, correlationId, failureCode, requestedFrom, requestedTo),
            pageNumber,
            pageSize,
            cancellationToken));
    }

    [HttpGet("review/pending")]
    public async Task<IActionResult> GetPendingReview(
        [FromQuery] PendingReviewSortBy sortBy = PendingReviewSortBy.ReviewRequiredAt,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await depositService.GetPendingReviewAsync(sortBy, descending, pageNumber, pageSize, cancellationToken));
    }

    [HttpPost("{transactionId}/review/retry-compensation")]
    public async Task<IActionResult> RetryCompensation(
        string transactionId,
        [FromBody] RetryDepositReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await depositService.RetryPendingReviewAsync(transactionId, request, cancellationToken));
        }
        catch (DepositNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidDepositReviewActionException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Deposit review action is invalid",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("{transactionId}/review/resolve")]
    public async Task<IActionResult> ResolvePendingReview(
        string transactionId,
        [FromBody] ResolveDepositReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await depositService.ResolvePendingReviewAsync(transactionId, request, cancellationToken));
        }
        catch (DepositNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidDepositReviewActionException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Deposit review action is invalid",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
