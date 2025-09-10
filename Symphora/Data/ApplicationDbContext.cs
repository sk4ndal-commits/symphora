using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Symphora.Models;

namespace Symphora.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Workflow> Workflows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime('now')");
        });
        
        // Configure Workflow
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.NodesJson)
                .IsRequired()
                .HasDefaultValue("[]");
                
            entity.Property(e => e.EdgesJson)
                .IsRequired()
                .HasDefaultValue("[]");
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime('now')");
                
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}