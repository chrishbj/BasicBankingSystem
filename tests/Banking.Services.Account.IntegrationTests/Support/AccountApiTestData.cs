using Banking.Services.Account.Contracts;

namespace Banking.Services.Account.IntegrationTests.Support;

internal static class AccountApiTestData
{
    public static OpenAccountRequest OpenActiveCheckingAccount()
        => new("cus_active_001", "Checking", "USD");

    public static ApplyDepositRequest CreateDeposit(decimal amount, string postingReference, string correlationId)
        => new(amount, "USD", postingReference, correlationId);

    public static CreateWithdrawalRequest CreateWithdrawal(decimal amount, string referenceNumber, string correlationId, string note = "atm withdrawal")
        => new(amount, "USD", referenceNumber, correlationId, note);
}
