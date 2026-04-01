using Banking.Bff.CustomerPortal.Auth;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Bff.CustomerPortal.Controllers;

[ApiController]
[Route("api/customer-portal/auth")]
public sealed class AuthController(
    CustomerServiceClient customerServiceClient,
    ICustomerPortalSessionService sessionService,
    ICurrentPortalCustomerAccessor currentPortalCustomerAccessor) : ControllerBase
{
    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn([FromBody] CustomerPortalSignInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await customerServiceClient.SignInAsync(request, cancellationToken);
            await sessionService.SignInAsync(customer, cancellationToken);
            return Ok(MapCustomer(customer));
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await sessionService.SignOutAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            return Ok(MapCustomer(await customerServiceClient.GetByIdAsync(customerId, cancellationToken)));
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    private static object MapCustomer(CustomerResponse customer) => new
    {
        customer.CustomerNumber,
        customer.FullName,
        customer.IdentityType,
        customer.IdentityNumberMasked,
        customer.PortalIdentityLast4,
        customer.Mobile,
        customer.Email,
        customer.RiskLevel,
        customer.Status,
        customer.CreatedAt,
        customer.UpdatedAt
    };
}
