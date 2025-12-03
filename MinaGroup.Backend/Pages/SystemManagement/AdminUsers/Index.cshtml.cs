using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.SystemManagement.AdminUsers
{
    [Authorize(Roles = "SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Liste over alle admin-brugere
        public IList<AdminUserViewModel> AdminUsers { get; set; } = [];

        // Formular-data
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Dropdown med organisationer
        public List<SelectListItem> Organizations { get; set; } = [];

        public class AdminUserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; } = string.Empty;
            public string? OrganizationName { get; set; }
        }

        public class InputModel
        {
            public string? Id { get; set; }

            [Required(ErrorMessage = "Fornavn er påkrævet")]
            [Display(Name = "Fornavn")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Efternavn er påkrævet")]
            [Display(Name = "Efternavn")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email er påkrævet")]
            [EmailAddress(ErrorMessage = "Indtast en gyldig email-adresse")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Indtast et gyldigt telefonnummer")]
            [Display(Name = "Telefonnummer")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Organisation")]
            [Required(ErrorMessage = "Vælg en organisation")]
            public int? OrganizationId { get; set; }

            // Kun til oprettelse / nulstilling – optional ved edit
            [DataType(DataType.Password)]
            [Display(Name = "Password (tom = ingen ændring)")]
            public string? Password { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            await LoadOrganizationsAsync();
            await LoadAdminUsersAsync();

            // Edit-mode: hvis der er et id i route
            if (!string.IsNullOrEmpty(id))
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    if (isAdmin)
                    {
                        Input = new InputModel
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            OrganizationId = user.OrganizationId
                        };
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            await LoadOrganizationsAsync();
            await LoadAdminUsersAsync();

            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Input.Password))
            {
                ModelState.AddModelError(nameof(Input.Password), "Password er påkrævet ved oprettelse.");
                return Page();
            }

            try
            {
                var user = new AppUser
                {
                    UserName = Input.Email.Trim(),
                    Email = Input.Email.Trim(),
                    FirstName = Input.FirstName.Trim(),
                    LastName = Input.LastName.Trim(),
                    PhoneNumber = Input.PhoneNumber?.Trim(),
                    OrganizationId = Input.OrganizationId
                };

                var createResult = await _userManager.CreateAsync(user, Input.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return Page();
                }

                // Sørg for at Admin-rollen findes
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    if (!roleResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Kunne ikke oprette Admin-rolle.");
                        return Page();
                    }
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return Page();
                }

                TempData["SuccessMessage"] = "Admin-bruger oprettet.";
                return RedirectToPage(); // tilbage til liste / tom formular
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved oprettelse af admin-bruger.");
                ModelState.AddModelError(string.Empty, "Der opstod en uventet fejl ved oprettelse af admin-brugeren.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            await LoadOrganizationsAsync();
            await LoadAdminUsersAsync();

            if (!ModelState.IsValid || string.IsNullOrEmpty(Input.Id))
                return Page();

            try
            {
                var user = await _userManager.FindByIdAsync(Input.Id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Brugeren blev ikke fundet.";
                    return RedirectToPage();
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    TempData["ErrorMessage"] = "Brugeren er ikke i rollen Admin.";
                    return RedirectToPage();
                }

                // Opdater basisfelter
                user.FirstName = Input.FirstName.Trim();
                user.LastName = Input.LastName.Trim();
                user.Email = Input.Email.Trim();
                user.UserName = Input.Email.Trim();
                user.PhoneNumber = Input.PhoneNumber?.Trim();
                user.OrganizationId = Input.OrganizationId;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return Page();
                }

                // Hvis password er udfyldt, nulstilles det via reset-token
                if (!string.IsNullOrWhiteSpace(Input.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var pwdResult = await _userManager.ResetPasswordAsync(user, token, Input.Password);

                    if (!pwdResult.Succeeded)
                    {
                        foreach (var error in pwdResult.Errors)
                            ModelState.AddModelError(nameof(Input.Password), error.Description);

                        return Page();
                    }
                }

                TempData["SuccessMessage"] = "Admin-brugeren blev opdateret.";
                return RedirectToPage(new { id = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved opdatering af admin-bruger {UserId}", Input.Id);
                ModelState.AddModelError(string.Empty, "Der opstod en uventet fejl ved opdatering af admin-brugeren.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            await LoadOrganizationsAsync();
            await LoadAdminUsersAsync();

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Intet bruger-ID angivet til sletning.";
                return RedirectToPage();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Brugeren blev ikke fundet.";
                    return RedirectToPage();
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    TempData["ErrorMessage"] = "Brugeren er ikke i rollen Admin.";
                    return RedirectToPage();
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return Page();
                }

                TempData["SuccessMessage"] = "Admin-brugeren blev slettet.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved sletning af admin-bruger {UserId}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved sletning af admin-brugeren.";
                return RedirectToPage();
            }
        }

        private async Task LoadOrganizationsAsync()
        {
            Organizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"{o.Name} ({o.CVRNumber})"
                })
                .ToListAsync();
        }

        private async Task LoadAdminUsersAsync()
        {
            // Hent alle brugere i rollen Admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Hent relevante organisationer
            var orgIds = admins
                .Where(a => a.OrganizationId.HasValue)
                .Select(a => a.OrganizationId!.Value)
                .Distinct()
                .ToList();

            var orgs = await _context.Organizations
                .Where(o => orgIds.Contains(o.Id))
                .ToListAsync();

            AdminUsers = admins
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName)
                .Select(a =>
                {
                    var orgName = a.OrganizationId.HasValue
                        ? orgs.FirstOrDefault(o => o.Id == a.OrganizationId.Value)?.Name
                        : null;

                    return new AdminUserViewModel
                    {
                        Id = a.Id,
                        FullName = $"{a.FirstName} {a.LastName}",
                        Email = a.Email,
                        PhoneNumber = a.PhoneNumber,
                        OrganizationName = orgName
                    };
                })
                .ToList();
        }
    }
}