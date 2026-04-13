namespace Banking.Services.Deposit.Contracts;

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
