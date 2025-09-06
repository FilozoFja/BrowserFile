using BrowserFile.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BrowserFile.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Folder> Folders { get; set; }
        public DbSet<Icon> Icons { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Folder>()
                .HasOne(f => f.User)
                .WithMany(u => u.Folders)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Folder>()
                .HasOne(f => f.Icon)
                .WithMany(i => i.Folders)
                .HasForeignKey(f => f.IconId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Folder>()
                .Property(f => f.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<Folder>()
                .HasIndex(f => new { f.UserId, f.Name })
                .IsUnique();
        }
    }
}
