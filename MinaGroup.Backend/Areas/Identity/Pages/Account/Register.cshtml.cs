using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Areas.Identity.Pages.Account
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
                             ICryptoService cryptoService, ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _cryptoService = cryptoService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public List<string?> AllRoles { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "CPR-nummer")]
            [RegularExpression(@"^\d{6}-\d{4}$", ErrorMessage = "CPR skal være 10 cifre i formatet xxxxxx-xxxx")]
            public string? PersonNumberCPR { get; set; }

            [Required]
            [Display(Name = "Fornavn")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Efternavn")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Telefonnummer")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Job startdato")]
            [DataType(DataType.Date)]
            public DateTime? JobStartDate { get; set; }

            [Display(Name = "Job slutdato")]
            [DataType(DataType.Date)]
            public DateTime? JobEndDate { get; set; }

            [Display(Name = "Arbejdsdage")]
            public WeekDays? ScheduledDays { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Bekræft password")]
            [Compare("Password", ErrorMessage = "Password og bekræftelse stemmer ikke overens")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            // Filtrer roller efter hvem der er logget ind
            if (User.IsInRole("SysAdmin"))
            {
                AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            }
            else if (User.IsInRole("Admin"))
            {
                AllRoles = _roleManager.Roles
                    .Where(r => r.Name == "Admin" || r.Name == "Leder" || r.Name == "Borger")
                    .Select(r => r.Name)
                    .ToList();
            }
            else
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<string> SelectedRoles, List<WeekDays> SelectedDays)
        {
            // Re-populate roles on error
            if (!ModelState.IsValid)
            {
                OnGet(); // fill AllRoles appropriately
                return Page();
            }

            try
            {
                var user = new AppUser
                {
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Email = Input.Email,
                    UserName = Input.Email,
                    PhoneNumber = Input.PhoneNumber,
                    JobStartDate = Input.JobStartDate,
                    JobEndDate = Input.JobEndDate,
                    ScheduledDays = (SelectedDays != null && SelectedDays.Any())
                        ? SelectedDays.Aggregate(WeekDays.None, (acc, d) => acc | d)
                        : WeekDays.None
                };

                // Encrypt CPR før gem
                user.EncryptedPersonNumber = string.IsNullOrWhiteSpace(Input.PersonNumberCPR)
                    ? null
                    : _cryptoService.Protect(Input.PersonNumberCPR);

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    OnGet();
                    return Page();
                }

                // Sikkerhed: Admin må kun tildele begrænsede roller
                if (User.IsInRole("Admin") && SelectedRoles != null)
                {
                    SelectedRoles = SelectedRoles.Where(r => r == "Admin" || r == "Leder" || r == "Borger").ToList();
                }

                if (SelectedRoles != null && SelectedRoles.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, SelectedRoles);
                }

                TempData["SuccessMessage"] = "Brugeroplysninger er oprettet.";
                return RedirectToPage("/Management/UserManagement/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved oprettelse af bruger");
                ModelState.AddModelError("", "Der opstod en fejl ved oprettelse af brugeren. Kontakt systemadministrator.");
                OnGet();
                return Page();
            }
        }
    }
}