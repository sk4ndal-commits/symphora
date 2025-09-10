using System.ComponentModel.DataAnnotations;

namespace Symphora.Models;

public class ExecutionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid WorkflowId { get; set; }
    
    [Required]
    public Guid AgentId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Running, Completed, Failed, Cancelled
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string Output { get; set; } = string.Empty;
    
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
}