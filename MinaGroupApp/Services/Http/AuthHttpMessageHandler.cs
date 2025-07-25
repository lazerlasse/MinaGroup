using MinaGroupApp.Models.Auth;
using MinaGroupApp.Services.Interfaces;
using System.Net.Http.Headers;

namespace MinaGroupApp.Services.Http;

public class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IAuthApi _authApi;

    public AuthHttpMessageHandler(ISecureStorageService secureStorage, IAuthApi authApi)
    {
        _secureStorage = secureStorage;
        _authApi = authApi;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? accessToken = await _secureStorage.GetAccessTokenAsync();
        DateTime? accessTokenExpiry = await _secureStorage.GetAccessTokenExpiryAsync();

        if (!string.IsNullOrEmpty(accessToken) && accessTokenExpiry.HasValue && accessTokenExpiry > DateTime.UtcNow)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else
        {
            string? refreshToken = await _secureStorage.GetRefreshTokenAsync();
            DateTime? refreshExpiry = await _secureStorage.GetRefreshTokenExpiryAsync();

            if (!string.IsNullOrEmpty(refreshToken) && refreshExpiry.HasValue && refreshExpiry > DateTime.UtcNow)
            {
                try
                {
                    var response = await _authApi.RefreshTokenAsync(new RefreshTokenRequestDto
                    {
                        RefreshToken = refreshToken
                    });

                    // Gem de nye tokens
                    await _secureStorage.SetAccessTokenAsync(response.AccessToken, DateTime.UtcNow.AddMinutes(15));
                    await _secureStorage.SetRefreshTokenAsync(response.RefreshToken, response.RefreshTokenExpires);

                    // Brug det nye access token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);
                }
                catch
                {
                    await _secureStorage.ClearAllAsync();
                    // Brugeren skal muligvis logges ud
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}