using Banking.Bff.CustomerPortal.Contracts;
namespace Banking.Bff.CustomerPortal.Auth;

public sealed class CustomerPortalSessionService(IHttpContextAccessor httpContextAccessor) : ICustomerPortalSessionService
{
    public async Task SignInAsync(CustomerResponse customer, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("An active HTTP context is required.");
        httpContext.Session.SetString(CustomerPortalSessionKeys.CustomerId, customer.CustomerId);
        httpContext.Session.SetString(CustomerPortalSessionKeys.CustomerNumber, customer.CustomerNumber);
        httpContext.Session.SetString(CustomerPortalSessionKeys.FullName, customer.FullName);
        await httpContext.Session.CommitAsync(cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("An active HTTP context is required.");
        httpContext.Session.Remove(CustomerPortalSessionKeys.CustomerId);
        httpContext.Session.Remove(CustomerPortalSessionKeys.CustomerNumber);
        httpContext.Session.Remove(CustomerPortalSessionKeys.FullName);
        httpContext.Session.Clear();
        await httpContext.Session.CommitAsync(cancellationToken);
    }
}
