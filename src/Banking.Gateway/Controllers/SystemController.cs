using Microsoft.AspNetCore.Mvc;

namespace Banking.Gateway.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            service = "Banking.Gateway",
            version = "v1",
            mode = "backend-only-skeleton"
        });
    }
}
