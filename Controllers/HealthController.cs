using Microsoft.AspNetCore.Mvc;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            ok = true,
            service = "CUChatNet.Api",
            utc = DateTime.UtcNow
        });
    }
}
