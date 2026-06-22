using Microsoft.AspNetCore.Identity;

namespace TrieuDoanKy_W2.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Age { get; set; }
    }
}
