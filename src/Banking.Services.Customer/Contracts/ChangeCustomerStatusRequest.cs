using Banking.Services.Customer.Domain;

namespace Banking.Services.Customer.Contracts;

public sealed record ChangeCustomerStatusRequest(
    CustomerStatus TargetStatus,
    string Reason);
