using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    [Authorize(Roles = "Admin,SysAdmin,Leder")]
    public class CreateModel : PageModel
    {
        private readonly MinaGroup.Backend.Data.AppDbContext _context;

        public CreateModel(MinaGroup.Backend.Data.AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public TaskOption TaskOption { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.TaskOptions.Add(TaskOption);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
