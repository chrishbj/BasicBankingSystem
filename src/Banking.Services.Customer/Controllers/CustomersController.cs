using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Banking.Services.Customer.Contracts;
using Banking.Services.Customer.Exceptions;
using Banking.Services.Customer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Services.Customer.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Policy = BankingPolicies.ExternalOrInternal)]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await customerService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { customerId = customer.CustomerId }, customer);
        }
        catch (DuplicateCustomerException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate customer",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetById(string customerId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.GetByIdAsync(customerId, cancellationToken));
        }
        catch (CustomerNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await customerService.GetAllAsync(pageNumber, pageSize, cancellationToken));
    }

    [HttpPost("portal-sign-in")]
    public async Task<IActionResult> PortalSignIn([FromBody] CustomerPortalSignInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.SignInForPortalAsync(request, cancellationToken));
        }
        catch (InvalidCustomerPortalSignInException)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid sign-in details",
                Detail = "The supplied customer number and identity number last 4 digits do not match an existing customer.",
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    [HttpPost("{customerId}/status")]
    public async Task<IActionResult> ChangeStatus(
        string customerId,
        [FromBody] ChangeCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.ChangeStatusAsync(customerId, request, cancellationToken));
        }
        catch (CustomerNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidCustomerStatusTransitionException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid status transition",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
