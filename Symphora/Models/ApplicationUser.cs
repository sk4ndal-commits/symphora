using Microsoft.AspNetCore.Identity;

namespace Symphora.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}