using Banking.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Gateway.Controllers;

[ApiController]
[Route("")]
public sealed class ProxyController(GatewayProxyService proxyService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("customer-api/swagger")]
    public IActionResult RedirectCustomerSwagger() => Redirect("/customer-api/swagger/index.html");

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("customer-api/api/v1/health")]
    public Task ProxyCustomerHealth(CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "customer-service", "api/v1/health", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("customer-api/swagger/{**downstreamPath}")]
    public Task ProxyCustomerSwagger(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "customer-service", $"swagger/{downstreamPath}", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("customer-api/openapi/{**downstreamPath}")]
    public Task ProxyCustomerOpenApi(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "customer-service", $"openapi/{downstreamPath}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
    [Route("customer-api/{**downstreamPath}")]
    public Task ProxyCustomer(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "customer-service", downstreamPath, cancellationToken);

    [AllowAnonymous]
    [HttpGet("account-api/swagger")]
    public IActionResult RedirectAccountSwagger() => Redirect("/account-api/swagger/index.html");

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("account-api/api/v1/health")]
    public Task ProxyAccountHealth(CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "account-service", "api/v1/health", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("account-api/swagger/{**downstreamPath}")]
    public Task ProxyAccountSwagger(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "account-service", $"swagger/{downstreamPath}", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("account-api/openapi/{**downstreamPath}")]
    public Task ProxyAccountOpenApi(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "account-service", $"openapi/{downstreamPath}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
    [Route("account-api/{**downstreamPath}")]
    public Task ProxyAccount(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "account-service", downstreamPath, cancellationToken);

    [AllowAnonymous]
    [HttpGet("deposit-api/swagger")]
    public IActionResult RedirectDepositSwagger() => Redirect("/deposit-api/swagger/index.html");

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("deposit-api/api/v1/health")]
    public Task ProxyDepositHealth(CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "deposit-service", "api/v1/health", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("deposit-api/swagger/{**downstreamPath}")]
    public Task ProxyDepositSwagger(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "deposit-service", $"swagger/{downstreamPath}", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("deposit-api/openapi/{**downstreamPath}")]
    public Task ProxyDepositOpenApi(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "deposit-service", $"openapi/{downstreamPath}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
    [Route("deposit-api/{**downstreamPath}")]
    public Task ProxyDeposit(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "deposit-service", downstreamPath, cancellationToken);

    [AllowAnonymous]
    [HttpGet("audit-api/swagger")]
    public IActionResult RedirectAuditSwagger() => Redirect("/audit-api/swagger/index.html");

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("audit-api/api/v1/health")]
    public Task ProxyAuditHealth(CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "audit-service", "api/v1/health", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("audit-api/swagger/{**downstreamPath}")]
    public Task ProxyAuditSwagger(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "audit-service", $"swagger/{downstreamPath}", cancellationToken);

    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [Route("audit-api/openapi/{**downstreamPath}")]
    public Task ProxyAuditOpenApi(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "audit-service", $"openapi/{downstreamPath}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
    [Route("audit-api/{**downstreamPath}")]
    public Task ProxyAudit(string? downstreamPath, CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(HttpContext, "audit-service", downstreamPath, cancellationToken);
}
