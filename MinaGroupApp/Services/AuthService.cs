using MinaGroupApp.DataTransferObjects.Auth;
using MinaGroupApp.Services.Interfaces;

namespace MinaGroupApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthApi _authApi;
        private readonly ISecureStorageService _secureStorage;

        public AuthService(IAuthApi authApi, ISecureStorageService secureStorage)
        {
            _authApi = authApi;
            _secureStorage = secureStorage;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _authApi.LoginAsync(new LoginRequestDto
                {
                    Email = email,
                    Password = password
                });

                await _secureStorage.SetAccessTokenAsync(response.Token, response.Expires);
                await _secureStorage.SetRefreshTokenAsync(response.RefreshToken, response.RefreshTokenExpires);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            var token = await _secureStorage.GetAccessTokenAsync();
            var expiry = await _secureStorage.GetAccessTokenExpiryAsync();

            if (!string.IsNullOrEmpty(token) && expiry.HasValue && expiry.Value > DateTime.UtcNow)
                return true;

            var refreshToken = await _secureStorage.GetRefreshTokenAsync();
            var refreshExpiry = await _secureStorage.GetRefreshTokenExpiryAsync();

            if (string.IsNullOrEmpty(refreshToken) || !refreshExpiry.HasValue || refreshExpiry.Value < DateTime.UtcNow)
                return false;

            try
            {
                var refreshResponse = await _authApi.RefreshTokenAsync(new RefreshTokenRequestDto() { RefreshToken = refreshToken });

                await _secureStorage.SetAccessTokenAsync(refreshResponse.AccessToken, DateTime.UtcNow.AddMinutes(15));
                await _secureStorage.SetRefreshTokenAsync(refreshResponse.RefreshToken, refreshResponse.RefreshTokenExpires);

                return true;
            }
            catch
            {
                await _secureStorage.ClearAllAsync();
                return false;
            }
        }
    }
}