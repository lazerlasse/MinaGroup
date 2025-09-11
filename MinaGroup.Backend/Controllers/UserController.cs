using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MinaGroup.Backend.DataTransferObjects;
using MinaGroup.Backend.Models;
using System.Security.Claims;

namespace MinaGroup.Backend.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Best practice: hent brugeren fra context
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var dto = new UserProfileResponseDto
            {
                DisplayName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty
            };

            return Ok(dto);
        }
    }
}
