using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Shandis.Api.Controllers
{
    [ApiController]
    [Route("api/auth-test")]
    [Authorize]
    public class AuthTestController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = User.FindFirstValue(ClaimTypes.Email),
                IsAuthenticated = User.Identity?.IsAuthenticated
            });
        }
    }
}
