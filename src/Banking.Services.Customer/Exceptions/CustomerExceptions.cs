namespace Banking.Services.Customer.Exceptions;

public sealed class DuplicateCustomerException(string message) : Exception(message);

public sealed class CustomerNotFoundException(string customerId)
    : Exception($"Customer '{customerId}' was not found.");

public sealed class InvalidCustomerStatusTransitionException(string message) : Exception(message);
