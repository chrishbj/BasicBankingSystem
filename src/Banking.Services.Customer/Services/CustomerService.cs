using Banking.BuildingBlocks.Contracts;
using Banking.Services.Customer.Contracts;
using Banking.Services.Customer.Exceptions;
using Banking.Services.Customer.Repositories;
using System.Text;

namespace Banking.Services.Customer.Services;

public sealed class CustomerService(ICustomerRepository repository) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByIdentityAsync(request.IdentityType, request.IdentityNumber, cancellationToken))
        {
            throw new DuplicateCustomerException("Identity number already exists.");
        }

        if (await repository.ExistsByMobileAsync(request.Mobile, null, cancellationToken))
        {
            throw new DuplicateCustomerException("Mobile already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var customer = new Domain.Customer
        {
            CustomerId = $"cus_{Guid.NewGuid():N}",
            CustomerNumber = $"C{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
            FullName = request.FullName.Trim(),
            IdentityType = request.IdentityType.Trim(),
            IdentityNumber = request.IdentityNumber.Trim(),
            Mobile = request.Mobile.Trim(),
            Email = request.Email?.Trim(),
            Address = new Domain.Address(
                request.Address.Country.Trim(),
                request.Address.Province.Trim(),
                request.Address.City.Trim(),
                request.Address.Line1.Trim(),
                request.Address.PostalCode.Trim()),
            RiskLevel = request.RiskLevel.Trim(),
            Status = Domain.CustomerStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(customer, cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerResponse> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException(customerId);

        return Map(customer);
    }

    public async Task<PagedResponse<CustomerSummaryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var customers = await repository.GetAllAsync(cancellationToken);
        var totalCount = customers.Count;
        var items = customers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(customer => new CustomerSummaryResponse(
                customer.CustomerId,
                customer.CustomerNumber,
                customer.FullName,
                customer.Mobile,
                customer.Status,
                customer.CreatedAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<CustomerSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<CustomerResponse> ChangeStatusAsync(
        string customerId,
        ChangeCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException(customerId);

        if (!IsTransitionAllowed(customer.Status, request.TargetStatus))
        {
            throw new InvalidCustomerStatusTransitionException(
                $"Cannot change status from '{customer.Status}' to '{request.TargetStatus}'.");
        }

        customer.ChangeStatus(request.TargetStatus, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(customer, cancellationToken);

        return Map(customer);
    }

    public async Task<CustomerResponse> SignInForPortalAsync(
        CustomerPortalSignInRequest request,
        CancellationToken cancellationToken)
    {
        var customerNumber = request.CustomerNumber.Trim();
        var identityLast4 = request.IdentityLast4.Trim();

        var customer = await repository.GetByCustomerNumberAsync(customerNumber, cancellationToken);
        if (customer is null || identityLast4.Length != 4 || !string.Equals(GetIdentityLast4Digits(customer.IdentityNumber), identityLast4, StringComparison.Ordinal))
        {
            throw new InvalidCustomerPortalSignInException();
        }

        return Map(customer);
    }

    private static bool IsTransitionAllowed(Domain.CustomerStatus currentStatus, Domain.CustomerStatus targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (Domain.CustomerStatus.Pending, Domain.CustomerStatus.Active) => true,
            (Domain.CustomerStatus.Active, Domain.CustomerStatus.Frozen) => true,
            (Domain.CustomerStatus.Frozen, Domain.CustomerStatus.Active) => true,
            (Domain.CustomerStatus.Active, Domain.CustomerStatus.Closed) => true,
            _ when currentStatus == targetStatus => true,
            _ => false
        };
    }

    private static CustomerResponse Map(Domain.Customer customer)
    {
        return new CustomerResponse(
            customer.CustomerId,
            customer.CustomerNumber,
            customer.FullName,
            customer.IdentityType,
            MaskIdentity(customer.IdentityNumber),
            customer.Mobile,
            customer.Email,
            new AddressResponse(
                customer.Address.Country,
                customer.Address.Province,
                customer.Address.City,
                customer.Address.Line1,
                customer.Address.PostalCode),
            customer.RiskLevel,
            customer.Status,
            customer.CreatedAt,
            customer.UpdatedAt);
    }

    private static string MaskIdentity(string identityNumber)
    {
        if (identityNumber.Length <= 8)
        {
            return "********";
        }

        return $"{identityNumber[..6]}********{identityNumber[^4..]}";
    }

    private static string GetIdentityLast4Digits(string identityNumber)
    {
        var digits = new StringBuilder();

        foreach (var character in identityNumber)
        {
            if (char.IsDigit(character))
            {
                digits.Append(character);
            }
        }

        if (digits.Length == 0)
        {
            return "0000";
        }

        var digitString = digits.ToString();
        if (digitString.Length >= 4)
        {
            return digitString[^4..];
        }

        return digitString.PadLeft(4, '0');
    }
}
