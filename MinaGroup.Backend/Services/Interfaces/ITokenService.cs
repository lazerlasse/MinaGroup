using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(AppUser user, IList<string> roles);
        (string Token, DateTime Expires) GenerateRefreshToken();
    }
}
