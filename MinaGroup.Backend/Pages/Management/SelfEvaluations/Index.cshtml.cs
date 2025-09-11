using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,SysAdmin,Leder")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<SelfEvaluation> SelfEvaluation { get;set; } = default!;

        public async Task OnGetAsync()
        {
            SelfEvaluation = await _context.SelfEvaluations
                .Include(s => s.User).ToListAsync();
        }
    }
}
