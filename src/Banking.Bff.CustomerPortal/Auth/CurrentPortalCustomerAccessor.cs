namespace Banking.Bff.CustomerPortal.Auth;

public sealed class CurrentPortalCustomerAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentPortalCustomerAccessor
{
    public string GetRequiredCustomerId()
    {
        var customerId = httpContextAccessor.HttpContext?.User.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidOperationException("The current request does not have an authenticated portal customer.");
        }

        return customerId;
    }
}
