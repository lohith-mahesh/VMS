using Microsoft.AspNetCore.Mvc;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        return Ok(new { id = currentUserService.UserId, name = currentUserService.Name, email = currentUserService.Email, role = currentUserService.Role });
    }
}