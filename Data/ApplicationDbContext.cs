using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Madtorio.Data.Models;

namespace Madtorio.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SaveFile> SaveFiles { get; set; }
    public DbSet<RuleCategory> RuleCategories { get; set; }
    public DbSet<Rule> Rules { get; set; }
    public DbSet<ServerConfig> ServerConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rule-RuleCategory relationship
        modelBuilder.Entity<Rule>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Rules)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create unique index on ServerConfig.Key
        modelBuilder.Entity<ServerConfig>()
            .HasIndex(sc => sc.Key)
            .IsUnique();
    }
}
