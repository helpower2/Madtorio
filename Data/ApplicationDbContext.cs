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
    public DbSet<DownloadLog> DownloadLogs { get; set; }
    public DbSet<PageView> PageViews { get; set; }
    public DbSet<ModRequest> ModRequests { get; set; }
    public DbSet<ModRequestVote> ModRequestVotes { get; set; }
    public DbSet<ModRequestLog> ModRequestLogs { get; set; }

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

        // DownloadLog-SaveFile relationship
        modelBuilder.Entity<DownloadLog>()
            .HasOne(dl => dl.SaveFile)
            .WithMany(sf => sf.DownloadLogs)
            .HasForeignKey(dl => dl.SaveFileId)
            .OnDelete(DeleteBehavior.Cascade);

        // ModRequestVote-ModRequest relationship
        modelBuilder.Entity<ModRequestVote>()
            .HasOne(v => v.ModRequest)
            .WithMany(r => r.Votes)
            .HasForeignKey(v => v.ModRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one vote per user per mod
        modelBuilder.Entity<ModRequestVote>()
            .HasIndex(v => new { v.ModRequestId, v.Username })
            .IsUnique();

        // ModRequestLog-ModRequest relationship
        modelBuilder.Entity<ModRequestLog>()
            .HasOne(l => l.ModRequest)
            .WithMany()
            .HasForeignKey(l => l.ModRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
