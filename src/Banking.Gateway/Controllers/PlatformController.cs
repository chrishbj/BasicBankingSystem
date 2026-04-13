using Banking.BuildingBlocks.Security;
using Banking.BuildingBlocks.Observability;
using Banking.Gateway.Contracts;
using Banking.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Gateway.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(Policy = BankingPolicies.PlatformReadOnly)]
public sealed class PlatformController(
    PlatformMonitoringService platformMonitoringService,
    PlatformMaintenanceService platformMaintenanceService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetOverviewAsync(cancellationToken));
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetServicesAsync(cancellationToken));
    }

    [HttpGet("compatibility")]
    public async Task<IActionResult> GetCompatibility(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetCompatibilityAsync(cancellationToken));
    }

    [HttpGet("rollouts")]
    public async Task<IActionResult> GetRollouts(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetRolloutsAsync(cancellationToken));
    }

    [HttpGet("environments")]
    public async Task<IActionResult> GetEnvironments(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetEnvironmentsAsync(cancellationToken));
    }

    [HttpGet("workflows/deposits/summary")]
    public async Task<IActionResult> GetDepositWorkflowSummary(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetDepositWorkflowSummaryAsync(cancellationToken));
    }

    [HttpGet("workflows/deposits/pending-review")]
    public async Task<IActionResult> GetPendingReview(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetPendingReviewAsync(cancellationToken));
    }

    [HttpGet("workflows/deposits/outbox")]
    public async Task<IActionResult> GetDepositOutboxMessages(
        [FromQuery] bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await platformMonitoringService.GetDepositOutboxMessagesAsync(pendingOnly, cancellationToken));
    }

    [HttpGet("workflows/deposits/runtime")]
    public async Task<IActionResult> GetDepositRuntimeStatus(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetDepositRuntimeStatusAsync(cancellationToken));
    }

    [HttpGet("workflows/deposits/{transactionId}")]
    public async Task<IActionResult> GetDepositWorkflowDetail(string transactionId, CancellationToken cancellationToken)
    {
        var detail = await platformMonitoringService.GetDepositWorkflowDetailAsync(transactionId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("diagnostics/correlation/{correlationId}")]
    public async Task<IActionResult> GetCorrelationDiagnostics(string correlationId, CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetCorrelationDiagnosticsAsync(correlationId, cancellationToken));
    }

    [HttpGet("audit/operations")]
    public async Task<IActionResult> GetPlatformOperationsAudit(CancellationToken cancellationToken)
    {
        return Ok(await platformMonitoringService.GetPlatformOperationsAuditAsync(cancellationToken));
    }

    [HttpPost("maintenance/deposits/{transactionId}/retry-compensation")]
    [Authorize(Policy = BankingPolicies.PlatformOperatorOnly)]
    public async Task<IActionResult> RetryDepositCompensation(
        string transactionId,
        [FromBody] RetryDepositCompensationRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.Identity?.Name ?? "platform-operator";
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
        var response = await platformMaintenanceService.RetryDepositCompensationAsync(
            transactionId,
            actorId,
            request.Reason,
            correlationId,
            cancellationToken);

        return StatusCode(
            response.ResultStatus == "Succeeded" ? StatusCodes.Status200OK : response.DownstreamStatusCode,
            response);
    }

    [HttpPost("maintenance/deposits/{transactionId}/resolve-review")]
    [Authorize(Policy = BankingPolicies.PlatformOperatorOnly)]
    public async Task<IActionResult> ResolveDepositReview(
        string transactionId,
        [FromBody] ResolveDepositReviewRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.Identity?.Name ?? "platform-operator";
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
        var response = await platformMaintenanceService.ResolveDepositReviewAsync(
            transactionId,
            actorId,
            request.Resolution,
            request.Reason,
            correlationId,
            cancellationToken);

        return StatusCode(
            response.ResultStatus == "Succeeded" ? StatusCodes.Status200OK : response.DownstreamStatusCode,
            response);
    }

    [HttpPost("maintenance/deposits/outbox/{messageId}/requeue")]
    [Authorize(Policy = BankingPolicies.PlatformOperatorOnly)]
    public async Task<IActionResult> RequeueDepositOutboxMessage(
        string messageId,
        [FromBody] RequeueOutboxMessageRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.Identity?.Name ?? "platform-operator";
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
        var response = await platformMaintenanceService.RequeueOutboxMessageAsync(
            messageId,
            actorId,
            request.Reason,
            correlationId,
            cancellationToken);

        return StatusCode(
            response.ResultStatus == "Succeeded" ? StatusCodes.Status200OK : response.DownstreamStatusCode,
            response);
    }
}
