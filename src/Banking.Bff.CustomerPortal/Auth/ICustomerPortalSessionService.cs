using Banking.Bff.CustomerPortal.Contracts;

namespace Banking.Bff.CustomerPortal.Auth;

public interface ICustomerPortalSessionService
{
    Task SignInAsync(CustomerResponse customer, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
