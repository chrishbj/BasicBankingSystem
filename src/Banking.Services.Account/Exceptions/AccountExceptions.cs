namespace Banking.Services.Account.Exceptions;

public sealed class AccountNotFoundException(string accountId)
    : Exception($"Account '{accountId}' was not found.");

public sealed class CustomerNotEligibleForAccountOpeningException(string customerId, string reason)
    : Exception($"Customer '{customerId}' is not eligible for account opening. {reason}");

public sealed class AccountNotEligibleForDepositException(string accountId, string reason)
    : Exception($"Account '{accountId}' is not eligible for deposit. {reason}");

public sealed class AccountDepositCompensationException(string accountId, string reason)
    : Exception($"Account '{accountId}' deposit compensation failed. {reason}");
