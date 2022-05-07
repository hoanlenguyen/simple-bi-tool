using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BITool.DBContext
{
    public class AdminUser : IdentityUser<int>
    {
        [MaxLength(200)]
        public string FirstName { get; set; }

        [MaxLength(200)]
        public string LastName { get; set; }
    }
}