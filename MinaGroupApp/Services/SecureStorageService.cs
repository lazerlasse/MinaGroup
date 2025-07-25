using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services
{
    public class SecureStorageService : ISecureStorageService
    {
        private const string AccessTokenKey = "access_token";
        private const string AccessTokenExpiryKey = "access_token_expiry";
        private const string RefreshTokenKey = "refresh_token";
        private const string RefreshTokenExpiryKey = "refresh_token_expiry";
        private const string UserNameKey = "username";


        // Access Token Section...
        public async Task SetAccessTokenAsync(string token, DateTime expiresAt)
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, token);
            await SecureStorage.Default.SetAsync(AccessTokenExpiryKey, expiresAt.ToString("O"));
        }

        public async Task<string?> GetAccessTokenAsync() =>
            await SecureStorage.Default.GetAsync(AccessTokenKey);

        public async Task<DateTime?> GetAccessTokenExpiryAsync()
        {
            var expiry = await SecureStorage.Default.GetAsync(AccessTokenExpiryKey);
            return DateTime.TryParse(expiry, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
        }



        // Refresh Token Section..
        public async Task SetRefreshTokenAsync(string token, DateTime expiresAt)
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, token);
            await SecureStorage.Default.SetAsync(RefreshTokenExpiryKey, expiresAt.ToString("O"));
        }

        public async Task<string?> GetRefreshTokenAsync() =>
            await SecureStorage.Default.GetAsync(RefreshTokenKey);

        public async Task<DateTime?> GetRefreshTokenExpiryAsync()
        {
            var expiry = await SecureStorage.Default.GetAsync(RefreshTokenExpiryKey);
            return DateTime.TryParse(expiry, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
        }



        // Clear All Set Data.
        public async Task ClearAllAsync()
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(AccessTokenExpiryKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(RefreshTokenExpiryKey);
            SecureStorage.Default.Remove(UserNameKey);
        }
    }
}
