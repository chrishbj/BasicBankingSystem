namespace Banking.Bff.CustomerPortal.Auth;

public interface ICurrentPortalCustomerAccessor
{
    string GetRequiredCustomerId();
}
