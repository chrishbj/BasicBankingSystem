using System.Text;
using Banking.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Gateway.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(GatewayHealthService gatewayHealthService) : ControllerBase
{
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            service = "Banking.Gateway",
            version = "v1",
            mode = "operations-gateway",
            routes = new
            {
                customer = "/customer-api",
                account = "/account-api",
                deposit = "/deposit-api",
                audit = "/audit-api"
            }
        });
    }

    [HttpGet("health-summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealthSummary(CancellationToken cancellationToken)
    {
        var summary = await gatewayHealthService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("docs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDocs(CancellationToken cancellationToken)
    {
        var summary = await gatewayHealthService.GetSummaryAsync(cancellationToken);
        var html = new StringBuilder();

        html.AppendLine("""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <title>Banking Gateway Docs</title>
              <style>
                body { font-family: Segoe UI, Arial, sans-serif; margin: 2rem; color: #1f2937; background: #f8fafc; }
                h1 { margin-bottom: 0.25rem; }
                p { color: #475569; }
                table { width: 100%; border-collapse: collapse; margin-top: 1.5rem; background: white; }
                th, td { text-align: left; padding: 0.85rem; border-bottom: 1px solid #e2e8f0; vertical-align: top; }
                th { background: #eff6ff; }
                .healthy { color: #047857; font-weight: 600; }
                .warning { color: #b45309; font-weight: 600; }
                .muted { color: #64748b; font-size: 0.9rem; }
                code { background: #f1f5f9; padding: 0.1rem 0.3rem; border-radius: 4px; }
              </style>
            </head>
            <body>
            """);

        html.AppendLine("<h1>Banking Gateway</h1>");
        html.AppendLine("<p>Operator-facing unified entry point for downstream service health, Swagger UI, and OpenAPI contracts.</p>");
        html.AppendLine($"""<p class="muted">Checked at: {summary.CheckedAt:yyyy-MM-dd HH:mm:ss zzz}</p>""");
        html.AppendLine("""
            <table>
              <thead>
                <tr>
                  <th>Service</th>
                  <th>Health</th>
                  <th>Base Path</th>
                  <th>Swagger</th>
                  <th>OpenAPI</th>
                </tr>
              </thead>
              <tbody>
            """);

        foreach (var service in summary.Services)
        {
            var toneClass = string.Equals(service.Health, "Healthy", StringComparison.OrdinalIgnoreCase)
                ? "healthy"
                : "warning";

            html.AppendLine($"""
                <tr>
                  <td><strong>{service.Name}</strong></td>
                  <td class="{toneClass}">{service.Health}</td>
                  <td><code>{service.BasePath}</code></td>
                  <td><a href="{service.SwaggerUrl}" target="_blank" rel="noreferrer">{service.SwaggerUrl}</a></td>
                  <td><a href="{service.OpenApiUrl}" target="_blank" rel="noreferrer">{service.OpenApiUrl}</a></td>
                </tr>
                """);
        }

        html.AppendLine("""
              </tbody>
            </table>
            </body>
            </html>
            """);

        return Content(html.ToString(), "text/html");
    }
}
