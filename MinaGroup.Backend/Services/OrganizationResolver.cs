using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Services
{
    public class OrganizationResolver : IOrganizationResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public OrganizationResolver(
            IHttpContextAccessor httpContextAccessor,
            UserManager<AppUser> userManager,
            AppDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _db = db;
        }

        public async Task<Organization?> GetCurrentOrganizationAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            var userId = _userManager.GetUserId(httpContext.User);
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _db.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Organization;
        }
    }
}
