using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICryptoService _cryptoService;

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

            // ✅ Fuldt CPR til intern visning i admin
            public string? PersonNumberCPR { get; set; } = string.Empty;

            public DateTime? JobStartDate { get; set; }
            public DateTime? JobEndDate { get; set; }
            public IList<string> Roles { get; set; } = [];

            // Flags enum
            public WeekDays? Weekdays { get; set; }

            // Læsevenlig ugeliste til UI
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

                // ✅ Hent fuldt CPR via fælles helper
                var fullCpr = CprHelper.GetFullCpr(user, _cryptoService);

                User = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    PersonNumberCPR = fullCpr,
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