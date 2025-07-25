using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services.Interfaces
{
    public interface ISecureStorageService
    {
        Task SetAccessTokenAsync(string token, DateTime expiresAt);
        Task<string?> GetAccessTokenAsync();
        Task<DateTime?> GetAccessTokenExpiryAsync();


        Task SetRefreshTokenAsync(string token, DateTime expiresAt);
        Task<string?> GetRefreshTokenAsync();
        Task<DateTime?> GetRefreshTokenExpiryAsync();

        Task ClearAllAsync();
    }
}
