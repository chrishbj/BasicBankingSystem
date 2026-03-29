using Banking.BuildingBlocks.Security;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Banking.BuildingBlocks.Swagger;

public sealed class BankingSecurityHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        // Swagger is used as a first-class manual testing surface in this repository,
        // so the required security headers are injected into every operation.
        AddHeaderParameter(
            operation,
            BankingAuthenticationDefaults.ApiKeyHeaderName,
            "External API access key for manual testing and client requests.");

        AddHeaderParameter(
            operation,
            BankingAuthenticationDefaults.ServiceNameHeaderName,
            "Internal caller service name for service-to-service requests.");

        AddHeaderParameter(
            operation,
            BankingAuthenticationDefaults.ServiceKeyHeaderName,
            "Internal caller service key for service-to-service requests.");
    }

    private static void AddHeaderParameter(OpenApiOperation operation, string headerName, string description)
    {
        var parameters = operation.Parameters ??= [];

        if (parameters.Any(parameter =>
                string.Equals(parameter.Name, headerName, StringComparison.OrdinalIgnoreCase) &&
                parameter.In == ParameterLocation.Header))
        {
            return;
        }

        parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            In = ParameterLocation.Header,
            Required = false,
            Description = description
        });
    }
}
