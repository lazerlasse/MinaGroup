using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MinaGroup.Backend.Pages.SystemManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}