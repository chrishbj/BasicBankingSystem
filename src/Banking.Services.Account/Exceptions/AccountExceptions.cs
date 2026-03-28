namespace Banking.Services.Account.Exceptions;

public sealed class AccountNotFoundException(string accountId)
    : Exception($"Account '{accountId}' was not found.");

public sealed class CustomerNotEligibleForAccountOpeningException(string customerId, string reason)
    : Exception($"Customer '{customerId}' is not eligible for account opening. {reason}");
