using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BITool.DBContext
{
    public class ApplicationDbContext : IdentityDbContext<AdminUser, AdminUserRole, int>
    {
        public ApplicationDbContext
           (DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AdminUser> AdminUser { get; set; }
        public DbSet<AdminUserRole> AdminUserRole { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<AdminUser>().ToTable("AdminUser");
            modelBuilder.Entity<AdminUserRole>().ToTable("AdminUserRole");
        }
    }
}
