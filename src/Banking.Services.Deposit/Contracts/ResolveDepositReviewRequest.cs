using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record ResolveDepositReviewRequest(
    DepositReviewResolution Resolution,
    string OperatorId,
    string Note);
