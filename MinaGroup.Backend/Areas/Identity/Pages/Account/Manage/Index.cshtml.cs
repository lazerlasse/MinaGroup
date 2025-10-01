using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using MinaGroup.Backend.Services.Interfaces; // ✅ tilføjet for EncryptionService

namespace MinaGroup.Backend.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ICryptoService _cryptoService; // ✅

        public IndexModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ICryptoService cryptoService) // ✅
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cryptoService = cryptoService;
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public IList<string> Roles { get; set; } = [];

        public class InputModel
        {
            [Display(Name = "Fornavn")]
            public string FirstName { get; set; } = string.Empty;

            [Display(Name = "Efternavn")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(11)]
            [RegularExpression(@"^\d{6}-\d{4}$", ErrorMessage = "CPR-nummer skal være 10 cifre i formatet xxxxxx-xxxx")]
            public string? PersonNumberCPR { get; set; } = null;

            [Display(Name = "Ansættelsesdato")]
            [DataType(DataType.Date)]
            public DateTime? JobStartDate { get; set; }

            [Display(Name = "Fratrædelsesdato")]
            [DataType(DataType.Date)]
            public DateTime? JobEndDate { get; set; }

            [Phone]
            [Display(Name = "Telefonnummer")]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        private async Task LoadAsync(AppUser user)
        {
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                // ✅ dekrypter CPR inden visning
                PersonNumberCPR = string.IsNullOrEmpty(user.EncryptedPersonNumber) ? null : _cryptoService.Unprotect(user.EncryptedPersonNumber),
                JobStartDate = user.JobStartDate,
                JobEndDate = user.JobEndDate
            };

            if (!string.IsNullOrEmpty(phoneNumber))
                Input.PhoneNumber = phoneNumber;

            Roles = roles;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kunne ikke indlæse bruger med ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kunne ikke indlæse bruger med ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // ✅ opdater felter
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.JobStartDate = Input.JobStartDate;
            user.JobEndDate = Input.JobEndDate;

            // ✅ krypter CPR inden gem
            if (!string.IsNullOrWhiteSpace(Input.PersonNumberCPR))
                user.EncryptedPersonNumber = _cryptoService.Protect(Input.PersonNumberCPR);

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Der opstod en fejl under opdatering af telefonnummer.";
                    return RedirectToPage();
                }
            }

            // ✅ gem ændringer
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Profilen er blevet opdateret";
            return RedirectToPage();
        }
    }
}