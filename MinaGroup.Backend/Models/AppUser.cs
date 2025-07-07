using Microsoft.AspNetCore.Identity;

namespace MinaGroup.Backend.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Role { get; set; } // "Admin" eller "Employee"
    }
}
