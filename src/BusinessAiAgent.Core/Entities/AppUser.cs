using Microsoft.AspNetCore.Identity;

namespace BusinessAiAgent.Core.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? ContactId { get; set; }
    public int? CompanyId { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contact? Contact { get; set; }
    public Company? Company { get; set; }
}
