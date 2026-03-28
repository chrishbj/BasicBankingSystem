namespace Banking.Services.Deposit.Exceptions;

public sealed class InvalidDepositRequestException(string message) : Exception(message);

public sealed class DepositNotFoundException(string transactionId)
    : Exception($"Deposit '{transactionId}' was not found.");
