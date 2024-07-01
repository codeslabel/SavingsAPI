using Microsoft.AspNetCore.Identity;

namespace SavingsAPI.Models
{
    public class User : IdentityUser
    {
        Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Roles { get; set; }
    }
}
