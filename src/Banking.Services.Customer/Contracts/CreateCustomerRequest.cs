namespace Banking.Services.Customer.Contracts;

public sealed record CreateCustomerRequest(
    string FullName,
    string IdentityType,
    string IdentityNumber,
    string Mobile,
    string? Email,
    AddressRequest Address,
    string RiskLevel);

public sealed record AddressRequest(
    string Country,
    string Province,
    string City,
    string Line1,
    string PostalCode);
