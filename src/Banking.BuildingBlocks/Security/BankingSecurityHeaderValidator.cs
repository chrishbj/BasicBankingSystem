using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Banking.BuildingBlocks.Security;

public sealed class BankingSecurityHeaderValidator(IOptionsMonitor<BankingSecurityOptions> optionsMonitor)
{
    public BankingSecurityValidationResult Validate(IHeaderDictionary headers)
    {
        var options = optionsMonitor.CurrentValue;
        var apiKey = headers[BankingAuthenticationDefaults.ApiKeyHeaderName].ToString();
        var serviceName = headers[BankingAuthenticationDefaults.ServiceNameHeaderName].ToString();
        var serviceKey = headers[BankingAuthenticationDefaults.ServiceKeyHeaderName].ToString();

        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var hasServiceName = !string.IsNullOrWhiteSpace(serviceName);
        var hasServiceKey = !string.IsNullOrWhiteSpace(serviceKey);

        if (hasServiceName || hasServiceKey)
        {
            if (!hasServiceName || !hasServiceKey)
            {
                return BankingSecurityValidationResult.Fail(
                    $"Both '{BankingAuthenticationDefaults.ServiceNameHeaderName}' and '{BankingAuthenticationDefaults.ServiceKeyHeaderName}' are required for internal service authentication.");
            }

            var matchedService = options.Authentication.InternalServices.FirstOrDefault(service =>
                string.Equals(service.Name, serviceName, StringComparison.Ordinal) &&
                string.Equals(service.ApiKey, serviceKey, StringComparison.Ordinal));

            if (matchedService is null)
            {
                return BankingSecurityValidationResult.Fail("The internal service credentials are invalid.");
            }

            return BankingSecurityValidationResult.Success(BankingPrincipalTypes.InternalService, matchedService.Name);
        }

        if (hasApiKey)
        {
            var matchedClient = options.Authentication.ExternalApiKeys.FirstOrDefault(client =>
                string.Equals(client.ApiKey, apiKey, StringComparison.Ordinal));

            if (matchedClient is null)
            {
                return BankingSecurityValidationResult.Fail("The API key is invalid.");
            }

            return BankingSecurityValidationResult.Success(BankingPrincipalTypes.ExternalClient, matchedClient.Name);
        }

        return BankingSecurityValidationResult.NoCredentials();
    }
}
