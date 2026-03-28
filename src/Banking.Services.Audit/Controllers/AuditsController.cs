using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Banking.Services.Audit.Contracts;
using Banking.Services.Audit.Exceptions;
using Banking.Services.Audit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Audit.Controllers;

[ApiController]
[Route("api/v1/audits")]
[Authorize(Policy = BankingPolicies.ExternalOrInternal)]
public sealed class AuditsController(IAuditService auditService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = BankingPolicies.InternalServiceOnly)]
    public async Task<IActionResult> Create([FromBody] CreateAuditLogRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var audit = await auditService.RecordAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { auditId = audit.AuditId }, audit);
        }
        catch (InvalidAuditLogException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid audit log request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{auditId}")]
    public async Task<IActionResult> GetById(string auditId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await auditService.GetByIdAsync(auditId, cancellationToken));
        }
        catch (AuditLogNotFoundException)
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
        return Ok(await auditService.GetAllAsync(pageNumber, pageSize, cancellationToken));
    }
}
