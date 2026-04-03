using FluentAssertions;
using YamlDotNet.RepresentationModel;

namespace Banking.Contracts.Tests;

public sealed class OpenApiContractTests
{
    [Fact]
    public void OpenApiDocument_Should_Be_Readable_As_Structured_Yaml()
    {
        var root = LoadRoot();

        root.Should().NotBeNull();
        GetMapping(root, "components").Should().NotBeNull();
        GetMapping(root, "paths").Should().NotBeNull();
    }

    [Fact]
    public void OpenApiDocument_Should_Contain_Current_Public_Service_Paths_And_Methods()
    {
        var paths = GetPaths();

        AssertPathOperations(paths, "/api/v1/customers", "get", "post");
        AssertPathOperations(paths, "/api/v1/customers/{customerId}", "get");
        AssertPathOperations(paths, "/api/v1/customers/portal-sign-in", "post");
        AssertPathOperations(paths, "/api/v1/customers/{customerId}/status", "post");

        AssertPathOperations(paths, "/api/v1/accounts", "get", "post");
        AssertPathOperations(paths, "/api/v1/accounts/{accountId}", "get");
        AssertPathOperations(paths, "/api/v1/accounts/by-number/{accountNumber}", "get");
        AssertPathOperations(paths, "/api/v1/accounts/{accountId}/withdrawals", "post");
        AssertPathOperations(paths, "/api/v1/accounts/{accountId}/activities", "get");

        AssertPathOperations(paths, "/api/v1/deposits", "get", "post");
        AssertPathOperations(paths, "/api/v1/deposits/{transactionId}", "get");
        AssertPathOperations(paths, "/api/v1/deposits/review/pending", "get");
        AssertPathOperations(paths, "/api/v1/deposits/{transactionId}/review/retry-compensation", "post");
        AssertPathOperations(paths, "/api/v1/deposits/{transactionId}/review/resolve", "post");

        AssertPathOperations(paths, "/api/v1/audits", "get");
        AssertPathOperations(paths, "/api/v1/audits/{auditId}", "get");

        AssertPathOperations(paths, "/api/v1/health", "get");
        AssertPathOperations(paths, "/api/v1/ready", "get");
    }

    [Fact]
    public void OpenApiDocument_Should_Not_Contain_Retired_Or_Out_Of_Scope_Paths()
    {
        var paths = GetPaths();

        paths.Children.Keys.Select(GetScalarValue).Should().NotContain([
            "/api/v1/customers/{customerId}/contact",
            "/api/v1/accounts/{accountId}/close",
            "/api/v1/deposits/by-number/{transactionNumber}",
            "/api/v1/deposits/review/demo",
            "/api/customer-portal/auth/sign-in",
            "/customer-api/api/v1/customers"
        ]);
    }

    [Fact]
    public void OpenApiDocument_Should_Define_Critical_Response_Codes()
    {
        var paths = GetPaths();

        GetResponses(paths, "/api/v1/customers", "post").Should().Contain(["201", "400", "409"]);
        GetResponses(paths, "/api/v1/customers/portal-sign-in", "post").Should().Contain(["200", "401"]);
        GetResponses(paths, "/api/v1/accounts/{accountId}/withdrawals", "post").Should().Contain(["200", "404", "409"]);
        GetResponses(paths, "/api/v1/deposits", "post").Should().Contain(["202", "400", "404", "409"]);
        GetResponses(paths, "/api/v1/deposits/review/pending", "get").Should().Contain(["200"]);
        GetResponses(paths, "/api/v1/deposits/{transactionId}/review/retry-compensation", "post").Should().Contain(["200", "404", "409"]);
        GetResponses(paths, "/api/v1/deposits/{transactionId}/review/resolve", "post").Should().Contain(["200", "404", "409"]);
        GetResponses(paths, "/api/v1/audits/{auditId}", "get").Should().Contain(["200", "404"]);
    }

    [Fact]
    public void OpenApiDocument_Should_Expose_Required_Security_Headers_For_Documented_Service_Apis()
    {
        var root = LoadRoot();
        var components = GetMapping(root, "components");
        var securitySchemes = GetMapping(components, "securitySchemes");
        var externalApiKey = GetMapping(securitySchemes, "externalApiKey");

        GetScalar(externalApiKey, "type").Should().Be("apiKey");
        GetScalar(externalApiKey, "in").Should().Be("header");
        GetScalar(externalApiKey, "name").Should().Be("X-Api-Key");

        var parameters = GetMapping(components, "parameters");
        GetScalar(GetMapping(parameters, "CorrelationIdHeader"), "name").Should().Be("X-Correlation-Id");
        GetScalar(GetMapping(parameters, "IdempotencyKeyHeader"), "name").Should().Be("Idempotency-Key");

        GetOperationParameters("/api/v1/customers", "post").Should().Contain("X-Correlation-Id");
        GetOperationParameters("/api/v1/deposits", "post").Should().Contain(["X-Correlation-Id", "Idempotency-Key"]);
    }

    [Fact]
    public void OpenApiDocument_Should_Define_ProblemDetails_And_Paged_Response_Schemas()
    {
        var schemas = GetSchemas();

        var problemDetails = GetMapping(schemas, "ProblemDetails");
        GetSequence(problemDetails, "required").Should().Contain(["title", "status"]);
        GetMapping(problemDetails, "properties").Children.Keys.Select(GetScalarValue).Should().Contain(["detail", "correlationId", "errors"]);

        AssertPagedSchema(schemas, "CustomerPagedResponse", "CustomerSummary");
        AssertPagedSchema(schemas, "AccountPagedResponse", "AccountSummary");
        AssertPagedSchema(schemas, "AccountActivityPagedResponse", "AccountActivity");
        AssertPagedSchema(schemas, "DepositPagedResponse", "DepositSummary");
        AssertPagedSchema(schemas, "PendingReviewDepositPagedResponse", "PendingReviewDepositSummary");
        AssertPagedSchema(schemas, "AuditPagedResponse", "AuditSummary");
    }

    [Fact]
    public void OpenApiDocument_Should_Define_Critical_Response_Schema_Fields()
    {
        var schemas = GetSchemas();

        AssertRequiredFields(
            schemas,
            "Customer",
            "customerId",
            "customerNumber",
            "fullName",
            "identityType",
            "identityNumberMasked",
            "portalIdentityLast4",
            "mobile",
            "address",
            "riskLevel",
            "status",
            "createdAt",
            "updatedAt");

        AssertRequiredFields(
            schemas,
            "Account",
            "accountId",
            "accountNumber",
            "customerId",
            "accountType",
            "currency",
            "status",
            "availableBalance",
            "ledgerBalance",
            "openedAt");

        AssertRequiredFields(
            schemas,
            "AccountActivity",
            "postingReference",
            "accountId",
            "postingType",
            "amount",
            "currency",
            "createdAt");

        AssertRequiredFields(
            schemas,
            "Deposit",
            "transactionId",
            "transactionNumber",
            "customerId",
            "accountId",
            "amount",
            "currency",
            "channel",
            "status",
            "accountPostingStatus",
            "auditStatus",
            "compensationStatus",
            "reviewResolution",
            "correlationId",
            "compensationRetryCount",
            "requestedAt");

        AssertRequiredFields(
            schemas,
            "PendingReviewDepositSummary",
            "transactionId",
            "transactionNumber",
            "customerId",
            "accountId",
            "accountNumber",
            "amount",
            "currency",
            "compensationStatus",
            "reviewResolution",
            "compensationRetryCount",
            "requestedAt");

        var auditDetail = GetMapping(schemas, "AuditDetail");
        GetNodeSequence(auditDetail, "allOf").Count.Should().Be(2);
    }

    [Fact]
    public void OpenApiDocument_Should_Define_Critical_Request_Schema_Fields_And_Constraints()
    {
        var schemas = GetSchemas();

        AssertRequiredFields(
            schemas,
            "CreateCustomerRequest",
            "fullName",
            "identityType",
            "identityNumber",
            "mobile",
            "address",
            "riskLevel");

        AssertPropertyScalar(schemas, "CreateCustomerRequest", "fullName", "maxLength", "200");
        AssertPropertyScalar(schemas, "CreateCustomerRequest", "identityType", "maxLength", "50");
        AssertPropertyScalar(schemas, "CreateCustomerRequest", "identityNumber", "maxLength", "50");
        AssertPropertyScalar(schemas, "CreateCustomerRequest", "mobile", "maxLength", "30");
        AssertPropertyScalar(schemas, "CreateCustomerRequest", "riskLevel", "maxLength", "30");

        AssertRequiredFields(
            schemas,
            "CreateWithdrawalRequest",
            "amount",
            "currency",
            "referenceNumber");

        AssertPropertyScalar(schemas, "CreateWithdrawalRequest", "currency", "minLength", "3");
        AssertPropertyScalar(schemas, "CreateWithdrawalRequest", "currency", "maxLength", "3");
        AssertPropertyScalar(schemas, "CreateWithdrawalRequest", "referenceNumber", "minLength", "1");
        AssertPropertyScalar(schemas, "CreateWithdrawalRequest", "referenceNumber", "maxLength", "100");

        AssertRequiredFields(
            schemas,
            "CreateDepositRequest",
            "customerId",
            "accountId",
            "amount",
            "currency",
            "channel");

        AssertPropertyScalar(schemas, "CreateDepositRequest", "currency", "minLength", "3");
        AssertPropertyScalar(schemas, "CreateDepositRequest", "currency", "maxLength", "3");
        AssertPropertyScalar(schemas, "CreateDepositRequest", "amount", "exclusiveMinimum", "0");

        AssertRequiredFields(
            schemas,
            "CustomerPortalSignInRequest",
            "customerNumber",
            "identityLast4");

        AssertPropertyScalar(schemas, "CustomerPortalSignInRequest", "identityLast4", "minLength", "4");
        AssertPropertyScalar(schemas, "CustomerPortalSignInRequest", "identityLast4", "maxLength", "4");

        AssertRequiredFields(
            schemas,
            "ResolveDepositReviewRequest",
            "resolution",
            "operatorId",
            "note");

        GetMapping(GetMapping(schemas, "RetryDepositReviewRequest"), "properties")
            .Children.Keys
            .Select(GetScalarValue)
            .Should()
            .Contain(["operatorId", "note"]);
    }

    [Fact]
    public void OpenApiDocument_Should_Define_Critical_Enums()
    {
        var schemas = GetSchemas();

        AssertEnum(schemas, "CustomerStatus", "Pending", "Active", "Frozen", "Closed");
        AssertEnum(schemas, "AccountStatus", "Active", "Frozen", "Closed");
        AssertEnum(schemas, "AccountPostingType", "DepositCredit", "DepositReversal", "WithdrawalDebit");
        AssertEnum(schemas, "DepositStatus", "Received", "Processing", "Succeeded", "Rejected", "Failed", "PendingReview");
        AssertEnum(schemas, "Channel", "Counter", "Online", "Atm", "Batch");
        AssertEnum(schemas, "DepositSagaStepStatus", "NotStarted", "InProgress", "Succeeded", "Failed", "Compensated", "Skipped");
        AssertEnum(schemas, "DepositReviewResolution", "None", "RetryRequested", "ReversedExternally", "FailedExternally");
        AssertEnum(schemas, "PendingReviewSortBy", "ReviewRequiredAt", "LastCompensationAttemptAt", "RequestedAt");
    }

    private static void AssertRequiredFields(YamlMappingNode schemas, string schemaName, params string[] expectedFields)
    {
        var schema = GetMapping(schemas, schemaName);

        GetSequence(schema, "required").Should().Contain(expectedFields);
        GetMapping(schema, "properties").Children.Keys.Select(GetScalarValue).Should().Contain(expectedFields);
    }

    private static void AssertPropertyScalar(
        YamlMappingNode schemas,
        string schemaName,
        string propertyName,
        string scalarName,
        string expectedValue)
    {
        var property = GetProperty(schemas, schemaName, propertyName);
        GetScalar(property, scalarName).Should().Be(expectedValue);
    }

    private static void AssertPagedSchema(YamlMappingNode schemas, string schemaName, string itemSchemaName)
    {
        var schema = GetMapping(schemas, schemaName);

        GetSequence(schema, "required").Should().Contain(["items", "pageNumber", "pageSize", "totalCount", "totalPages"]);

        var itemsProperty = GetMapping(GetMapping(schema, "properties"), "items");
        GetScalar(GetMapping(itemsProperty, "items"), "$ref").Should().Be($"#/components/schemas/{itemSchemaName}");
    }

    private static void AssertEnum(YamlMappingNode schemas, string schemaName, params string[] expectedValues)
    {
        var schema = GetMapping(schemas, schemaName);

        GetSequence(schema, "enum").Should().ContainInOrder(expectedValues);
    }

    private static void AssertPathOperations(YamlMappingNode paths, string path, params string[] operations)
    {
        var pathMap = GetMapping(paths, path);

        foreach (var operation in operations)
        {
            GetMapping(pathMap, operation).Should().NotBeNull();
        }
    }

    private static IReadOnlyCollection<string> GetResponses(YamlMappingNode paths, string path, string operation)
    {
        var responses = GetMapping(GetMapping(GetMapping(paths, path), operation), "responses");
        return responses.Children.Keys.Select(GetScalarValue).ToArray();
    }

    private static IReadOnlyCollection<string> GetOperationParameters(string path, string operation)
    {
        var operationMap = GetMapping(GetMapping(GetPaths(), path), operation);

        if (!operationMap.Children.TryGetValue(new YamlScalarNode("parameters"), out var parametersNode))
        {
            return [];
        }

        parametersNode.Should().BeOfType<YamlSequenceNode>();

        return ((YamlSequenceNode)parametersNode)
            .Children
            .Select(parameter => parameter is YamlMappingNode mapping && mapping.Children.TryGetValue(new YamlScalarNode("$ref"), out var refNode)
                ? GetScalarValue(refNode).Split('/').Last() switch
                {
                    "CorrelationIdHeader" => "X-Correlation-Id",
                    "IdempotencyKeyHeader" => "Idempotency-Key",
                    _ => GetScalarValue(refNode)
                }
                : parameter is YamlMappingNode inlineMapping
                    ? GetScalar(inlineMapping, "name")
                    : GetScalarValue(parameter))
            .ToArray();
    }

    private static YamlMappingNode GetPaths() => GetMapping(LoadRoot(), "paths");

    private static YamlMappingNode GetSchemas() => GetMapping(GetMapping(LoadRoot(), "components"), "schemas");

    private static YamlMappingNode GetProperty(YamlMappingNode schemas, string schemaName, string propertyName) =>
        GetMapping(GetMapping(GetMapping(schemas, schemaName), "properties"), propertyName);

    private static YamlMappingNode LoadRoot()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var openApiPath = Path.Combine(root, "docs", "openapi-phase1.yaml");

        File.Exists(openApiPath).Should().BeTrue();

        using var reader = new StreamReader(openApiPath);
        var yaml = new YamlStream();
        yaml.Load(reader);

        return (YamlMappingNode)yaml.Documents.Single().RootNode;
    }

    private static YamlMappingNode GetMapping(YamlMappingNode parent, string key)
    {
        parent.Children.TryGetValue(new YamlScalarNode(key), out var node).Should().BeTrue($"Expected key '{key}' to exist.");
        node.Should().BeOfType<YamlMappingNode>();
        return (YamlMappingNode)node;
    }

    private static string GetScalar(YamlMappingNode parent, string key)
    {
        parent.Children.TryGetValue(new YamlScalarNode(key), out var node).Should().BeTrue($"Expected key '{key}' to exist.");
        node.Should().BeOfType<YamlScalarNode>();
        return ((YamlScalarNode)node).Value.Should().NotBeNull().And.Subject;
    }

    private static IReadOnlyCollection<string> GetSequence(YamlMappingNode parent, string key)
    {
        parent.Children.TryGetValue(new YamlScalarNode(key), out var node).Should().BeTrue($"Expected key '{key}' to exist.");
        node.Should().BeOfType<YamlSequenceNode>();
        return ((YamlSequenceNode)node).Children.Select(GetScalarValue).ToArray();
    }

    private static IReadOnlyCollection<YamlNode> GetNodeSequence(YamlMappingNode parent, string key)
    {
        parent.Children.TryGetValue(new YamlScalarNode(key), out var node).Should().BeTrue($"Expected key '{key}' to exist.");
        node.Should().BeOfType<YamlSequenceNode>();
        return ((YamlSequenceNode)node).Children.ToArray();
    }

    private static string GetScalarValue(YamlNode node)
    {
        node.Should().BeOfType<YamlScalarNode>();
        return ((YamlScalarNode)node).Value.Should().NotBeNull().And.Subject;
    }
}
