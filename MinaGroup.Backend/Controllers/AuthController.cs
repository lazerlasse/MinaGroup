using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.DataTransferObjects.Auth;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;

    public AccountController(UserManager<AppUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }
    
    // POST: api/account/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Ugyldige loginoplysninger");

        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var (refreshToken, refreshTokenExpires) = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpires;

        await _userManager.UpdateAsync(user);

        return Ok(new LoginResponseDto
        {
            Token = accessToken,
            Expires = DateTime.UtcNow.AddMinutes(15),
            RefreshToken = refreshToken,
            RefreshTokenExpires = refreshTokenExpires
        });
    }

    // POST: api/account/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken.Token;
        user.RefreshTokenExpiryTime = newRefreshToken.Expires;

        await _userManager.UpdateAsync(user);

        return Ok(new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpires = newRefreshToken.Expires
        });
    }
}
