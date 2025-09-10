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
    public DbSet<Agent> Agents { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }

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
        
        // Configure Agent
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.ParametersJson)
                .IsRequired()
                .HasDefaultValue("{}");
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime('now')");
        });
        
        // Configure ExecutionLog
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("datetime('now')");
                
            entity.Property(e => e.Output)
                .IsRequired()
                .HasDefaultValue(string.Empty);
                
            entity.HasOne(e => e.Workflow)
                .WithMany()
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}