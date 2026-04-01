namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record CustomerResponse(
    string CustomerId,
    string CustomerNumber,
    string FullName,
    string IdentityType,
    string IdentityNumberMasked,
    string PortalIdentityLast4,
    string Mobile,
    string? Email,
    AddressResponse Address,
    string RiskLevel,
    int Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AddressResponse(
    string Country,
    string Province,
    string City,
    string Line1,
    string PostalCode);
