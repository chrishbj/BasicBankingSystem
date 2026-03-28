using Banking.Services.Customer.Domain;

namespace Banking.Services.Customer.Contracts;

public sealed record CustomerResponse(
    string CustomerId,
    string CustomerNumber,
    string FullName,
    string IdentityType,
    string IdentityNumberMasked,
    string Mobile,
    string? Email,
    AddressResponse Address,
    string RiskLevel,
    CustomerStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AddressResponse(
    string Country,
    string Province,
    string City,
    string Line1,
    string PostalCode);

public sealed record CustomerSummaryResponse(
    string CustomerId,
    string CustomerNumber,
    string FullName,
    string Mobile,
    CustomerStatus Status,
    DateTimeOffset CreatedAt);
