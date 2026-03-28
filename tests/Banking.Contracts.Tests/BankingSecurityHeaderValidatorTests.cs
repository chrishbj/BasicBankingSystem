using Banking.BuildingBlocks.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Banking.Contracts.Tests;

public sealed class BankingSecurityHeaderValidatorTests
{
    private readonly BankingSecurityHeaderValidator _validator;

    public BankingSecurityHeaderValidatorTests()
    {
        var options = new BankingSecurityOptions
        {
            Authentication = new BankingAuthenticationSettings
            {
                ExternalApiKeys =
                [
                    new ExternalApiKeyOptions
                    {
                        Name = "local-dev-client",
                        ApiKey = "local-dev-api-key"
                    }
                ],
                InternalServices =
                [
                    new InternalServiceKeyOptions
                    {
                        Name = "deposit-service",
                        ApiKey = "deposit-service-dev-key"
                    }
                ]
            }
        };

        _validator = new BankingSecurityHeaderValidator(new StaticOptionsMonitor<BankingSecurityOptions>(options));
    }

    [Fact]
    public void Validate_Should_Succeed_For_External_Api_Key()
    {
        var headers = new HeaderDictionary
        {
            [BankingAuthenticationDefaults.ApiKeyHeaderName] = "local-dev-api-key"
        };

        var result = _validator.Validate(headers);

        result.Succeeded.Should().BeTrue();
        result.PrincipalType.Should().Be(BankingPrincipalTypes.ExternalClient);
        result.PrincipalName.Should().Be("local-dev-client");
    }

    [Fact]
    public void Validate_Should_Succeed_For_Internal_Service_Headers()
    {
        var headers = new HeaderDictionary
        {
            [BankingAuthenticationDefaults.ServiceNameHeaderName] = "deposit-service",
            [BankingAuthenticationDefaults.ServiceKeyHeaderName] = "deposit-service-dev-key"
        };

        var result = _validator.Validate(headers);

        result.Succeeded.Should().BeTrue();
        result.PrincipalType.Should().Be(BankingPrincipalTypes.InternalService);
        result.PrincipalName.Should().Be("deposit-service");
    }

    [Fact]
    public void Validate_Should_Fail_When_Internal_Service_Header_Is_Incomplete()
    {
        var headers = new HeaderDictionary
        {
            [BankingAuthenticationDefaults.ServiceNameHeaderName] = "deposit-service"
        };

        var result = _validator.Validate(headers);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue { get; } = currentValue;

        public TOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
