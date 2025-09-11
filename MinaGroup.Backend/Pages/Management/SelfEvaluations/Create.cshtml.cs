using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,SysAdmin,Leder")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        public List<string> ArrivalOptions { get; } = ["Intet valgt" , "Til tiden", "Forsent", "Aftalt forsinkelse"];
        public List<string> CollaborationOptions { get; } = ["Intet valgt" , "Godt", "Okay", "Dårligt"];
        public List<string> AssistanceOptions { get; } = ["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"];
        public List<string> AidOptions { get; } = ["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"];


        public IActionResult OnGet()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName");
            return Page();
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.SelfEvaluations.Add(SelfEvaluation);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
