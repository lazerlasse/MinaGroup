using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services; // ✅ Tilføjet til ICryptoService
using MinaGroup.Backend.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICryptoService _cryptoService; // ✅ Kryptering

        public DetailsModel(UserManager<AppUser> userManager, ICryptoService cryptoService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        public new UserViewModel User { get; set; } = new UserViewModel();

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string? Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; } = string.Empty;

            // ✅ CPR vises dekrypteret i UI
            public string? PersonNumberCPR { get; set; } = string.Empty;

            public DateTime? JobStartDate { get; set; }
            public DateTime? JobEndDate { get; set; }
            public IList<string> Roles { get; set; } = [];

            // Flags enum
            public WeekDays? Weekdays { get; set; }

            // Read-only property til UI visning
            public List<string> WeekdaysList => Weekdays?.ToString().Split(", ").ToList() ?? [];
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound("Bruger-ID ikke angivet.");

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound($"Brugeren med ID '{id}' blev ikke fundet.");

                var roles = await _userManager.GetRolesAsync(user);

                // ✅ Dekrypter CPR (håndtering af fejl og null)
                string? decryptedCpr = null;
                if (!string.IsNullOrEmpty(user.EncryptedPersonNumber))
                {
                    try
                    {
                        decryptedCpr = _cryptoService.Unprotect(user.EncryptedPersonNumber);
                    }
                    catch
                    {
                        decryptedCpr = "[Fejl ved dekryptering]";
                    }
                }

                User = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    PersonNumberCPR = decryptedCpr,
                    JobStartDate = user.JobStartDate,
                    JobEndDate = user.JobEndDate,
                    Roles = roles,
                    Weekdays = user.ScheduledDays
                };

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Der opstod en fejl: {ex.Message}");
                return Page();
            }
        }
    }
}