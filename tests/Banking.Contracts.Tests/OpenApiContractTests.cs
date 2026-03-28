using FluentAssertions;

namespace Banking.Contracts.Tests;

public sealed class OpenApiContractTests
{
    [Fact]
    public async Task OpenApiDocument_Should_Contain_AllMvpPaths()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var openApiPath = Path.Combine(root, "docs", "openapi-phase1.yaml");

        File.Exists(openApiPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(openApiPath);

        content.Should().Contain("/api/v1/customers");
        content.Should().Contain("/api/v1/accounts");
        content.Should().Contain("/api/v1/deposits");
        content.Should().Contain("/api/v1/audits");
    }
}
