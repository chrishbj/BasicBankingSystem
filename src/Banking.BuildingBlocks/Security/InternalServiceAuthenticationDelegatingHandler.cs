using Microsoft.Extensions.Options;

namespace Banking.BuildingBlocks.Security;

public sealed class InternalServiceAuthenticationDelegatingHandler(
    IOptions<BankingSecurityOptions> options)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var currentService = options.Value.CurrentServiceIdentity;

        if (!string.IsNullOrWhiteSpace(currentService.ServiceName))
        {
            request.Headers.Remove(BankingAuthenticationDefaults.ServiceNameHeaderName);
            request.Headers.Add(BankingAuthenticationDefaults.ServiceNameHeaderName, currentService.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(currentService.ApiKey))
        {
            request.Headers.Remove(BankingAuthenticationDefaults.ServiceKeyHeaderName);
            request.Headers.Add(BankingAuthenticationDefaults.ServiceKeyHeaderName, currentService.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
