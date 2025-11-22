using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.ServiceManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Google Drive status til UI
        public bool HasGoogleDriveConfig { get; set; }
        public bool IsGoogleDriveEnabled { get; set; }
        public bool HasRefreshToken { get; set; }
        public string? GoogleDriveAccountEmail { get; set; }

        public async Task OnGetAsync()
        {
            var cfg = await _context.GoogleDriveConfigs.FirstOrDefaultAsync();

            if (cfg == null)
            {
                HasGoogleDriveConfig = false;
                IsGoogleDriveEnabled = false;
                HasRefreshToken = false;
                GoogleDriveAccountEmail = null;
                return;
            }

            HasGoogleDriveConfig = true;
            IsGoogleDriveEnabled = cfg.IsEnabled;
            HasRefreshToken = !string.IsNullOrWhiteSpace(cfg.EncryptedRefreshToken);
            GoogleDriveAccountEmail = cfg.ConnectedAccountEmail;
        }
    }
}
