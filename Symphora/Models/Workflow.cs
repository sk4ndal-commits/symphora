using System.ComponentModel.DataAnnotations;

namespace Symphora.Models;

public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public string NodesJson { get; set; } = "[]";
    
    public string EdgesJson { get; set; } = "[]";
    
    [Required]
    public string OwnerId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ApplicationUser Owner { get; set; } = null!;
}