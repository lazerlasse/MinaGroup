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
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<TaskOption> TaskOption { get; set; } = default!;

        public async Task OnGetAsync()
        {
            TaskOption = await _context.TaskOptions.ToListAsync();
        }
    }
}
