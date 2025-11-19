using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Helpers;              // ✅ CPR-helper
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICryptoService _cryptoService;

        public EditModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ICryptoService cryptoService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<string?> AllRoles { get; set; } = [];

        public class InputModel
        {
            public string Id { get; set; } = string.Empty;

            [StringLength(11)]
            [RegularExpression(@"^\d{6}-\d{4}$", ErrorMessage = "CPR-nummer skal være 10 cifre i formatet xxxxxx-xxxx")]
            public string? PersonNumberCPR { get; set; } = null;

            [EmailAddress(ErrorMessage = "Indtast en gyldig email-adresse")]
            [Display(Name = "Email")]
            public string? Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Fornavn er påkrævet")]
            [Display(Name = "Fornavn")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Efternavn er påkrævet")]
            [Display(Name = "Efternavn")]
            public string LastName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Indtast et gyldigt telefonnummer")]
            [Display(Name = "Telefonnummer")]
            public string? PhoneNumber { get; set; } = string.Empty;

            [Display(Name = "Start Dato")]
            public DateTime? JobStartDate { get; set; }

            [Display(Name = "Slut Dato")]
            public DateTime? JobEndDate { get; set; }

            [Display(Name = "Bruger Roller")]
            public IList<string> Roles { get; set; } = new List<string>();

            [Display(Name = "Arbejdsdage")]
            public WeekDays? ScheduledDays { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound("Bruger-ID ikke angivet.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Brugeren med ID '{id}' blev ikke fundet.");

            var userRoles = await _userManager.GetRolesAsync(user);

            // ✅ Brug CPR-helper til at hente fuldt, formatteret CPR (ddMMyy-xxxx)
            var fullCpr = CprHelper.GetFullCpr(user, _cryptoService);

            Input = new InputModel
            {
                Id = user.Id,
                PersonNumberCPR = fullCpr,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                JobStartDate = user.JobStartDate,
                JobEndDate = user.JobEndDate,
                Roles = userRoles,
                ScheduledDays = user.ScheduledDays
            };

            // ✅ Filtrer roller afhængigt af hvem der er logget ind
            AllRoles = _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => User.IsInRole("SysAdmin") || r != "SysAdmin")
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<string> SelectedRoles, List<WeekDays> SelectedDays)
        {
            if (!ModelState.IsValid)
            {
                AllRoles = _roleManager.Roles
                    .Select(r => r.Name)
                    .Where(r => User.IsInRole("SysAdmin") || r != "SysAdmin")
                    .ToList();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
                return NotFound($"Brugeren med ID '{Input.Id}' blev ikke fundet.");

            // ✅ Krypter CPR (CPR-helper bruges på læsesiden, her gemmer vi bare sikkert)
            if (!string.IsNullOrWhiteSpace(Input.PersonNumberCPR))
            {
                try
                {
                    user.EncryptedPersonNumber = _cryptoService.Protect(Input.PersonNumberCPR);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Input.PersonNumberCPR), $"Fejl ved kryptering: {ex.Message}");
                    AllRoles = _roleManager.Roles
                        .Select(r => r.Name)
                        .Where(r => User.IsInRole("SysAdmin") || r != "SysAdmin")
                        .ToList();
                    return Page();
                }
            }
            else
            {
                user.EncryptedPersonNumber = null;
            }

            // ✅ Opdater øvrige felter
            user.Email = Input.Email;
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.PhoneNumber = Input.PhoneNumber;
            user.JobStartDate = Input.JobStartDate;
            user.JobEndDate = Input.JobEndDate;

            // ✅ Saml valgte dage
            user.ScheduledDays = SelectedDays.Any()
                ? SelectedDays.Aggregate(WeekDays.None, (acc, d) => acc | d)
                : WeekDays.None;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                AllRoles = _roleManager.Roles
                    .Select(r => r.Name)
                    .Where(r => User.IsInRole("SysAdmin") || r != "SysAdmin")
                    .ToList();
                return Page();
            }

            // ✅ Roller
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Admin må ikke tilføje/fjerne SysAdmin
            if (!User.IsInRole("SysAdmin"))
            {
                SelectedRoles = SelectedRoles.Where(r => r != "SysAdmin").ToList();
            }

            var rolesToAdd = SelectedRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(SelectedRoles);

            if (rolesToAdd.Any())
                await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (rolesToRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["SuccessMessage"] = "Brugeroplysninger er opdateret.";
            return RedirectToPage("./Index");
        }
    }
}