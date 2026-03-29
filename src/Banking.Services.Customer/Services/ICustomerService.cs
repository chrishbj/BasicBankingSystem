using Banking.BuildingBlocks.Contracts;
using Banking.Services.Customer.Contracts;

namespace Banking.Services.Customer.Services;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse> GetByIdAsync(string customerId, CancellationToken cancellationToken);
    Task<PagedResponse<CustomerSummaryResponse>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CustomerResponse> ChangeStatusAsync(string customerId, ChangeCustomerStatusRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse> SignInForPortalAsync(CustomerPortalSignInRequest request, CancellationToken cancellationToken);
}
