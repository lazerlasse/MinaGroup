using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    [Authorize(Roles = "Admin,SysAdmin,Leder")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TaskOption TaskOption { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskoption = await _context.TaskOptions.FirstOrDefaultAsync(m => m.TaskOptionId == id);

            if (taskoption == null)
            {
                return NotFound();
            }
            else
            {
                TaskOption = taskoption;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskoption = await _context.TaskOptions.FindAsync(id);
            if (taskoption != null)
            {
                TaskOption = taskoption;
                _context.TaskOptions.Remove(TaskOption);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
