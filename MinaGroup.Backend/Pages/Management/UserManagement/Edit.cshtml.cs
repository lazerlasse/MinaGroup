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
    // 🔐 Denne side er nu KUN til Admin og Leder (SysAdmin bruger SystemManagement senere)
    [Authorize(Roles = "Admin,Leder")]
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

        /// <summary>
        /// Bygger den rolle-liste, der må vælges i UI afhængigt af hvem der er logget ind.
        /// Admin: Admin + Leder + Borger
        /// Leder: kun Borger
        /// </summary>
        private void PopulateRolesForCurrentUser()
        {
            if (User.IsInRole("Admin"))
            {
                AllRoles = _roleManager.Roles
                    .Where(r => r.Name == "Admin" || r.Name == "Leder" || r.Name == "Borger")
                    .Select(r => r.Name)
                    .ToList();
            }
            else if (User.IsInRole("Leder"))
            {
                AllRoles = _roleManager.Roles
                    .Where(r => r.Name == "Borger")
                    .Select(r => r.Name)
                    .ToList();
            }
            else
            {
                AllRoles = [];
            }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound("Bruger-ID ikke angivet.");

            // Hent den nuværende bruger (Admin/Leder)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid("Du er ikke tilknyttet en organisation.");

            // Hent brugeren der skal redigeres
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Brugeren med ID '{id}' blev ikke fundet.");

            // 🔒 Org-scope: må kun redigere brugere i samme org
            if (user.OrganizationId != currentUser.OrganizationId)
                return Forbid("Du har ikke adgang til at redigere denne bruger.");

            var userRoles = await _userManager.GetRolesAsync(user);

            // Leder må ikke redigere Admin/Leder
            if (User.IsInRole("Leder") &&
                (userRoles.Contains("Admin") || userRoles.Contains("Leder") || userRoles.Contains("SysAdmin")))
            {
                return Forbid("Leder må kun redigere borgere i egen organisation.");
            }

            PopulateRolesForCurrentUser();
            if (!AllRoles.Any())
                return Forbid();

            // ✅ Brug CPR-helper til fuldt, formatteret CPR (ddMMyy-xxxx)
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<string> SelectedRoles, List<WeekDays> SelectedDays)
        {
            PopulateRolesForCurrentUser();
            if (!AllRoles.Any())
                return Forbid();

            SelectedRoles ??= new List<string>();
            SelectedDays ??= new List<WeekDays>();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Hent den nuværende bruger (Admin/Leder)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid("Du er ikke tilknyttet en organisation.");

            // Hent brugeren der skal opdateres
            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
                return NotFound($"Brugeren med ID '{Input.Id}' blev ikke fundet.");

            // 🔒 Org-scope
            if (user.OrganizationId != currentUser.OrganizationId)
                return Forbid("Du har ikke adgang til at redigere denne bruger.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Leder må ikke redigere Admin/Leder/SysAdmin (dobbelt-tjek)
            if (User.IsInRole("Leder") &&
                (currentRoles.Contains("Admin") || currentRoles.Contains("Leder") || currentRoles.Contains("SysAdmin")))
            {
                return Forbid("Leder må kun redigere borgere i egen organisation.");
            }

            // ✅ Krypter CPR
            if (!string.IsNullOrWhiteSpace(Input.PersonNumberCPR))
            {
                try
                {
                    user.EncryptedPersonNumber = _cryptoService.Protect(Input.PersonNumberCPR);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Input.PersonNumberCPR), $"Fejl ved kryptering: {ex.Message}");
                    return Page();
                }
            }
            else
            {
                user.EncryptedPersonNumber = null;
            }

            // ✅ Opdater øvrige felter
            user.Email = Input.Email;
            user.UserName = Input.Email; // hold UserName i sync med Email
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.PhoneNumber = Input.PhoneNumber;
            user.JobStartDate = Input.JobStartDate;
            user.JobEndDate = Input.JobEndDate;

            // ✅ Saml valgte dage
            user.ScheduledDays = SelectedDays.Any()
                ? SelectedDays.Aggregate(WeekDays.None, (acc, d) => acc | d)
                : WeekDays.None;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            // ✅ Rolle-håndtering:
            // Leder → må KUN håndtere "Borger"
            if (User.IsInRole("Leder"))
            {
                SelectedRoles = SelectedRoles
                    .Where(r => r == "Borger")
                    .ToList();
            }
            // Admin → må kun Admin/Leder/Borger (ingen SysAdmin)
            else if (User.IsInRole("Admin"))
            {
                SelectedRoles = SelectedRoles
                    .Where(r => r == "Admin" || r == "Leder" || r == "Borger")
                    .ToList();
            }

            // Sørg for, at vi ikke prøver at fjerne roller, som leder ikke må røre
            var allowedRolesToWorkWith = new HashSet<string>(AllRoles.Where(r => r != null)!);

            var filteredCurrentRoles = currentRoles
                .Where(r => allowedRolesToWorkWith.Contains(r))
                .ToList();

            var rolesToAdd = SelectedRoles.Except(filteredCurrentRoles).ToList();
            var rolesToRemove = filteredCurrentRoles.Except(SelectedRoles).ToList();

            if (rolesToAdd.Any())
                await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (rolesToRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["SuccessMessage"] = "Brugeroplysninger er opdateret.";
            return RedirectToPage("./Index");
        }
    }
}