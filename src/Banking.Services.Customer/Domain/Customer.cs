namespace Banking.Services.Customer.Domain;

public sealed class Customer
{
    public string CustomerId { get; init; } = default!;
    public string CustomerNumber { get; init; } = default!;
    public string FullName { get; set; } = default!;
    public string IdentityType { get; init; } = default!;
    public string IdentityNumber { get; init; } = default!;
    public string Mobile { get; set; } = default!;
    public string? Email { get; set; }
    public Address Address { get; set; } = default!;
    public string RiskLevel { get; init; } = default!;
    public CustomerStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void UpdateContact(string mobile, string? email, Address address, DateTimeOffset updatedAt)
    {
        Mobile = mobile;
        Email = email;
        Address = address;
        UpdatedAt = updatedAt;
    }

    public void ChangeStatus(CustomerStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }
}

public sealed record Address(
    string Country,
    string Province,
    string City,
    string Line1,
    string PostalCode);
