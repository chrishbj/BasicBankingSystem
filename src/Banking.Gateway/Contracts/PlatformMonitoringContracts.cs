namespace Banking.Gateway.Contracts;

public sealed record PlatformServiceStatusResponse(
    string Name,
    string BasePath,
    string Health,
    int? StatusCode,
    string SwaggerUrl,
    string OpenApiUrl);

public sealed record PlatformDependencyStatusResponse(
    string Name,
    string Status,
    string CheckedBy);

public sealed record DepositPendingReviewItemResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    string CompensationStatus,
    string ReviewResolution,
    string? FailureCode,
    string? FailureReason,
    int CompensationRetryCount,
    string? ReviewLastActionBy,
    string? ReviewNote,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewRequiredAt,
    DateTimeOffset? LastCompensationAttemptAt,
    DateTimeOffset? LastProcessedAt);

public sealed record DepositOutboxMessageItemResponse(
    string MessageId,
    string TransactionId,
    string MessageType,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ProcessedAt,
    string? LastError);

public sealed record DepositWorkerRuntimeStatusResponse(
    string WorkerName,
    string Mode,
    bool Enabled,
    int PollingIntervalMilliseconds,
    int BacklogCount,
    string Notes);

public sealed record DepositRuntimeStatusResponse(
    DateTimeOffset CheckedAt,
    string MessageTransport,
    int PendingReviewCount,
    int PendingOutboxCount,
    IReadOnlyCollection<DepositWorkerRuntimeStatusResponse> Workers);

public sealed record DepositWorkflowSummaryResponse(
    DateTimeOffset CheckedAt,
    int ReceivedCount,
    int SucceededCount,
    int FailedCount,
    int PendingReviewCount,
    IReadOnlyCollection<DepositPendingReviewItemResponse> PendingReviewItems);

public sealed record DepositWorkflowDetailResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    string? ReferenceNumber,
    string Channel,
    string Status,
    string AccountPostingStatus,
    string AuditStatus,
    string CompensationStatus,
    string ReviewResolution,
    string CorrelationId,
    string? FailureCode,
    string? FailureReason,
    int CompensationRetryCount,
    string? ReviewLastActionBy,
    string? ReviewNote,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ReversedAt,
    DateTimeOffset? ReviewRequiredAt,
    DateTimeOffset? ReviewResolvedAt,
    DateTimeOffset? LastCompensationAttemptAt,
    DateTimeOffset? LastProcessedAt);

public sealed record AuditTraceItemResponse(
    string AuditId,
    string ActorType,
    string ActorId,
    string Action,
    string AggregateType,
    string AggregateId,
    string CorrelationId,
    DateTimeOffset OccurredAt);

public sealed record CorrelationDiagnosticsResponse(
    string CorrelationId,
    DateTimeOffset CheckedAt,
    IReadOnlyCollection<DepositWorkflowDetailResponse> Deposits,
    IReadOnlyCollection<AuditTraceItemResponse> AuditEvents);

public sealed record RetryDepositCompensationRequest(
    string Reason);

public sealed record ResolveDepositReviewRequest(
    string Resolution,
    string Reason);

public sealed record RequeueOutboxMessageRequest(
    string Reason);

public sealed record PlatformMaintenanceActionResponse(
    string OperationId,
    string ActionType,
    string TargetType,
    string TargetId,
    string ActorId,
    string ResultStatus,
    int DownstreamStatusCode,
    string Reason,
    DateTimeOffset OccurredAt);

public sealed record PlatformOverviewResponse(
    string Platform,
    DateTimeOffset CheckedAt,
    IReadOnlyCollection<PlatformServiceStatusResponse> Services,
    IReadOnlyCollection<PlatformDependencyStatusResponse> Dependencies,
    DepositWorkflowSummaryResponse Deposits,
    DepositRuntimeStatusResponse DepositRuntime);
