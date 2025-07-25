using MinaGroupApp.Models.Auth;
using Refit;

namespace MinaGroupApp.Services.Interfaces;

public interface IAuthApi
{
    // Login api call.
    [Post("/api/Account/login")]
    Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto request);

    // Refresh token call.
    [Post("/api/Account/refresh-token")]
    Task<RefreshTokenResponseDto> RefreshTokenAsync([Body] RefreshTokenRequestDto dto);
}
