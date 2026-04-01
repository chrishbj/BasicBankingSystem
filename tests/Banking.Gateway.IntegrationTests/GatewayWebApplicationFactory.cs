using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Gateway.IntegrationTests;

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public RecordingDownstreamStub CustomerStub { get; } = new("customer");
    public RecordingDownstreamStub AccountStub { get; } = new("account");
    public RecordingDownstreamStub DepositStub { get; } = new("deposit");
    public RecordingDownstreamStub AuditStub { get; } = new("audit");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:CustomerServiceBaseUrl"] = "http://customer.test/",
                ["Infrastructure:AccountServiceBaseUrl"] = "http://account.test/",
                ["Infrastructure:DepositServiceBaseUrl"] = "http://deposit.test/",
                ["Infrastructure:AuditServiceBaseUrl"] = "http://audit.test/",
                ["Security:Authentication:ExternalApiKeys:0:Name"] = "local-dev-client",
                ["Security:Authentication:ExternalApiKeys:0:ApiKey"] = "local-dev-api-key",
                ["Security:Authentication:ExternalApiKeys:0:PrincipalType"] = "business-user",
                ["Security:Authentication:ExternalApiKeys:0:Roles:0"] = "platform-operator",
                ["Security:Authentication:InternalServices:0:Name"] = "gateway-service",
                ["Security:Authentication:InternalServices:0:ApiKey"] = "gateway-service-dev-key",
                ["Security:Authentication:InternalServices:0:Roles:0"] = "internal-service",
                ["Security:CurrentServiceIdentity:ServiceName"] = "gateway-service",
                ["Security:CurrentServiceIdentity:ApiKey"] = "gateway-service-dev-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("customer-service", client => client.BaseAddress = new Uri("http://customer.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(CustomerStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("account-service", client => client.BaseAddress = new Uri("http://account.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(AccountStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("deposit-service", client => client.BaseAddress = new Uri("http://deposit.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(DepositStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("audit-service", client => client.BaseAddress = new Uri("http://audit.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(AuditStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();
        });
    }
}

public sealed class RecordingDownstreamStub(string serviceName)
{
    public ConcurrentQueue<RecordedDownstreamRequest> Requests { get; } = new();

    public Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(new RecordedDownstreamRequest(
            request.Method.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase)));

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (path == "/api/v1/health")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Healthy")
            });
        }

        if (serviceName == "customer" && path == "/api/v1/customers")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new[]
                    {
                        new
                        {
                            customerNumber = "C2026033114163272720",
                            fullName = "Gateway Demo Customer"
                        }
                    },
                    pageNumber = 1,
                    pageSize = 1,
                    totalCount = 1,
                    totalPages = 1
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits")
        {
            var query = QueryHelpers.ParseQuery(request.RequestUri?.Query ?? string.Empty);
            if (query.TryGetValue("correlationId", out var correlationIdValue) &&
                string.Equals(correlationIdValue.ToString(), "corr-platform-001", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        items = new[]
                        {
                            new
                            {
                                transactionId = "dep-platform-001",
                                transactionNumber = "D202604010101",
                                customerId = "cus_active_001",
                                accountId = "acc_active_001",
                                accountNumber = "6222200000000000001",
                                amount = 250.0m,
                                currency = "USD",
                                referenceNumber = "PLATFORM-REF-001",
                                channel = "Counter",
                                status = "PendingReview",
                                requestedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                                postedAt = DateTimeOffset.UtcNow.AddMinutes(-11)
                            }
                        },
                        pageNumber = 1,
                        pageSize = 20,
                        totalCount = 1,
                        totalPages = 1
                    })
                });
            }

            var status = query.TryGetValue("status", out var statusValue) ? statusValue.ToString() : null;
            var totalCount = status switch
            {
                "Received" => 2,
                "Succeeded" => 14,
                "Failed" => 1,
                _ => 0
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = Array.Empty<object>(),
                    pageNumber = 1,
                    pageSize = 1,
                    totalCount,
                    totalPages = totalCount == 0 ? 0 : totalCount
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/dep-platform-001")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    transactionId = "dep-platform-001",
                    transactionNumber = "D202604010101",
                    customerId = "cus_active_001",
                    accountId = "acc_active_001",
                    amount = 250.0m,
                    currency = "USD",
                    referenceNumber = "PLATFORM-REF-001",
                    channel = "Counter",
                    status = "PendingReview",
                    accountPostingStatus = "Succeeded",
                    auditStatus = "Failed",
                    compensationStatus = "Failed",
                    reviewResolution = "None",
                    correlationId = "corr-platform-001",
                    failureCode = "DEPOSIT_COMPENSATION_REVIEW_REQUIRED",
                    failureReason = "Compensation requires supervised retry.",
                    compensationRetryCount = 3,
                    reviewLastActionBy = (string?)null,
                    reviewNote = (string?)null,
                    requestedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                    postedAt = DateTimeOffset.UtcNow.AddMinutes(-11),
                    reversedAt = (DateTimeOffset?)null,
                    reviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-7),
                    reviewResolvedAt = (DateTimeOffset?)null,
                    lastCompensationAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                    lastProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/dep-platform-001/review/retry-compensation" && request.Method == HttpMethod.Post)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    transactionId = "dep-platform-001",
                    transactionNumber = "D202604010101",
                    customerId = "cus_active_001",
                    accountId = "acc_active_001",
                    amount = 250.0m,
                    currency = "USD",
                    referenceNumber = "PLATFORM-REF-001",
                    channel = "Counter",
                    status = "PendingReview",
                    accountPostingStatus = "Succeeded",
                    auditStatus = "Failed",
                    compensationStatus = "Failed",
                    reviewResolution = "None",
                    correlationId = "corr-platform-001",
                    failureCode = "DEPOSIT_COMPENSATION_REVIEW_REQUIRED",
                    failureReason = "Compensation retry requested from platform operations.",
                    compensationRetryCount = 4,
                    reviewLastActionBy = "local-dev-client",
                    reviewNote = "Platform maintenance retry",
                    requestedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                    postedAt = DateTimeOffset.UtcNow.AddMinutes(-11),
                    reversedAt = (DateTimeOffset?)null,
                    reviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-7),
                    reviewResolvedAt = (DateTimeOffset?)null,
                    lastCompensationAttemptAt = DateTimeOffset.UtcNow,
                    lastProcessedAt = DateTimeOffset.UtcNow
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/dep-platform-001/review/resolve" && request.Method == HttpMethod.Post)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    transactionId = "dep-platform-001",
                    transactionNumber = "D202604010101",
                    customerId = "cus_active_001",
                    accountId = "acc_active_001",
                    amount = 250.0m,
                    currency = "USD",
                    referenceNumber = "PLATFORM-REF-001",
                    channel = "Counter",
                    status = "Reversed",
                    accountPostingStatus = "Succeeded",
                    auditStatus = "Succeeded",
                    compensationStatus = "Compensated",
                    reviewResolution = "ReversedExternally",
                    correlationId = "corr-platform-001",
                    failureCode = "DEPOSIT_COMPENSATED_EXTERNALLY",
                    failureReason = "Platform resolved the pending review externally.",
                    compensationRetryCount = 4,
                    reviewLastActionBy = "local-dev-client",
                    reviewNote = "Platform resolved pending review",
                    requestedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                    postedAt = DateTimeOffset.UtcNow.AddMinutes(-11),
                    reversedAt = DateTimeOffset.UtcNow,
                    reviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-7),
                    reviewResolvedAt = DateTimeOffset.UtcNow,
                    lastCompensationAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                    lastProcessedAt = DateTimeOffset.UtcNow
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/review/pending")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new[]
                    {
                        new
                        {
                            transactionId = "dep-review-001",
                            transactionNumber = "D202604010001",
                            customerId = "cus_active_001",
                            accountId = "acc_active_001",
                            accountNumber = "6222200000000000001",
                            amount = 100.0m,
                            currency = "USD",
                            compensationStatus = "Failed",
                            reviewResolution = "None",
                            failureCode = "DEPOSIT_COMPENSATION_REVIEW_REQUIRED",
                            failureReason = "Compensation requires supervised retry.",
                            compensationRetryCount = 3,
                            reviewLastActionBy = (string?)null,
                            reviewNote = (string?)null,
                            requestedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                            reviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                            lastCompensationAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                            lastProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
                        }
                    },
                    pageNumber = 1,
                    pageSize = 10,
                    totalCount = 1,
                    totalPages = 1
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/outbox")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new object[]
                {
                    new
                    {
                        messageId = "out-platform-001",
                        transactionId = "dep-platform-001",
                        messageType = "DepositRequestedMessage",
                        occurredAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                        processedAt = (DateTimeOffset?)DateTimeOffset.UtcNow.AddMinutes(-11),
                        lastError = (string?)null
                    },
                    new
                    {
                        messageId = "out-platform-002",
                        transactionId = "dep-review-001",
                        messageType = "DepositRequestedMessage",
                        occurredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                        processedAt = (DateTimeOffset?)null,
                        lastError = (string?)null
                    }
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/runtime/status")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    checkedAt = DateTimeOffset.UtcNow,
                    messageTransport = "InMemory",
                    pendingReviewCount = 1,
                    pendingOutboxCount = 1,
                    workers = new object[]
                    {
                        new
                        {
                            workerName = "DepositPendingReviewRetryWorker",
                            mode = "AutomaticCompensationRetry",
                            enabled = true,
                            pollingIntervalMilliseconds = 2000,
                            backlogCount = 0,
                            notes = "Max automatic retries: 3."
                        },
                        new
                        {
                            workerName = "DepositOutboxDispatcher",
                            mode = "OutboxDispatch",
                            enabled = true,
                            pollingIntervalMilliseconds = 500,
                            backlogCount = 1,
                            notes = "Transport: InMemory. Queue: basic-banking.deposit.requested."
                        }
                    }
                })
            });
        }

        if (serviceName == "deposit" && path == "/api/v1/deposits/outbox/out-platform-002/requeue" && request.Method == HttpMethod.Post)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    messageId = "out-platform-002",
                    transactionId = "dep-review-001",
                    messageType = "DepositRequestedMessage",
                    occurredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    processedAt = (DateTimeOffset?)null,
                    lastError = (string?)null
                })
            });
        }

        if (serviceName == "audit" && path == "/api/v1/audits")
        {
            if (request.Method == HttpMethod.Post)
            {
                var requestBody = request.Content is null
                    ? string.Empty
                    : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                var action = requestBody.Contains("PlatformMaintenanceResolveDepositReview", StringComparison.Ordinal)
                    ? "PlatformMaintenanceResolveDepositReview"
                    : requestBody.Contains("PlatformMaintenanceRequeueOutboxMessage", StringComparison.Ordinal)
                        ? "PlatformMaintenanceRequeueOutboxMessage"
                        : "PlatformMaintenanceRetryCompensation";
                var auditId = action switch
                {
                    "PlatformMaintenanceResolveDepositReview" => "aud-platform-maint-002",
                    "PlatformMaintenanceRequeueOutboxMessage" => "aud-platform-maint-003",
                    _ => "aud-platform-maint-001"
                };

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(new
                    {
                        auditId,
                        actorType = "PlatformOperator",
                        actorId = "local-dev-client",
                        action,
                        aggregateType = "DepositTransaction",
                        aggregateId = "dep-platform-001",
                        beforeSnapshot = (object?)null,
                        afterSnapshot = new
                        {
                            reason = "Platform maintenance retry",
                            resultStatus = "Succeeded"
                        },
                        correlationId = "corr-platform-001",
                        causationId = "platform-maintenance-dep-platform-001"
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new object[]
                    {
                        new
                        {
                            auditId = "aud-platform-maint-001",
                            actorType = "PlatformOperator",
                            actorId = "local-dev-client",
                            action = "PlatformMaintenanceRetryCompensation",
                            aggregateType = "DepositTransaction",
                            aggregateId = "dep-platform-001",
                            correlationId = "corr-platform-001",
                            occurredAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                        },
                        new
                        {
                            auditId = "aud-platform-maint-002",
                            actorType = "PlatformOperator",
                            actorId = "local-dev-client",
                            action = "PlatformMaintenanceResolveDepositReview",
                            aggregateType = "DepositTransaction",
                            aggregateId = "dep-platform-001",
                            correlationId = "corr-platform-001",
                            occurredAt = DateTimeOffset.UtcNow
                        },
                        new
                        {
                            auditId = "aud-platform-maint-003",
                            actorType = "PlatformOperator",
                            actorId = "local-dev-client",
                            action = "PlatformMaintenanceRequeueOutboxMessage",
                            aggregateType = "DepositOutboxMessage",
                            aggregateId = "out-platform-002",
                            correlationId = "corr-platform-001",
                            occurredAt = DateTimeOffset.UtcNow.AddSeconds(10)
                        },
                        new
                        {
                            auditId = "aud-platform-001",
                            actorType = "System",
                            actorId = "deposit-service",
                            action = "DepositCompensationPendingReview",
                            aggregateType = "DepositTransaction",
                            aggregateId = "dep-platform-001",
                            correlationId = "corr-platform-001",
                            occurredAt = DateTimeOffset.UtcNow.AddMinutes(-2)
                        },
                        new
                        {
                            auditId = "aud-platform-002",
                            actorType = "System",
                            actorId = "deposit-service",
                            action = "DepositCreated",
                            aggregateType = "DepositTransaction",
                            aggregateId = "dep-platform-001",
                            correlationId = "corr-platform-001",
                            occurredAt = DateTimeOffset.UtcNow.AddMinutes(-12)
                        },
                        new
                        {
                            auditId = "aud-platform-003",
                            actorType = "System",
                            actorId = "deposit-service",
                            action = "UnrelatedAudit",
                            aggregateType = "DepositTransaction",
                            aggregateId = "dep-other-001",
                            correlationId = "corr-other-001",
                            occurredAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                        }
                    },
                    pageNumber = 1,
                    pageSize = 100,
                    totalCount = 6,
                    totalPages = 1
                })
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public sealed record RecordedDownstreamRequest(
    string Method,
    string PathAndQuery,
    IReadOnlyDictionary<string, string[]> Headers);

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request, cancellationToken);
}
