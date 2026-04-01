namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record CustomerDashboardResponse(
    CustomerSnapshotResponse Customer,
    PortfolioSummaryResponse Portfolio,
    AccountSnapshotResponse? CurrentAccount,
    ActivitySnapshotResponse? LatestActivity,
    IReadOnlyList<RecentActivityResponse> RecentActivities,
    IReadOnlyList<TransactionStatusSummaryResponse> RecentTransactions);

public sealed record CustomerSnapshotResponse(
    string CustomerNumber,
    string FullName,
    int Status,
    string RiskLevel);

public sealed record PortfolioSummaryResponse(
    int AccountCount,
    decimal TotalAvailableBalance,
    decimal TotalLedgerBalance);

public sealed record AccountSnapshotResponse(
    string AccountNumber,
    string AccountType,
    int Status,
    string Currency,
    decimal AvailableBalance,
    decimal LedgerBalance);

public sealed record ActivitySnapshotResponse(
    string Type,
    string Reference,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record RecentActivityResponse(
    string AccountNumber,
    string Type,
    string Reference,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record TransactionStatusSummaryResponse(
    string TransactionNumber,
    string AccountNumber,
    decimal Amount,
    string Currency,
    int Status,
    string? ReferenceNumber,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt,
    string? FailureCode,
    string? FailureReason);
