using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MinaGroup.Backend.Pages.Management
{
    [Authorize(Roles = "Admin,Leder")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
