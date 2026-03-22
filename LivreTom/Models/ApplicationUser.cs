using Microsoft.AspNetCore.Identity;

namespace LivreTom.Models;

public class ApplicationUser : IdentityUser
{
    public int Credits { get; set; } = 0;
    public string? DisplayName { get; set; }
    public DateTime? DisplayNameLastChangedAt { get; set; }
    public DateTime? ScheduledDeletionAt { get; set; }

    public bool CanChangeDisplayName =>
        DisplayNameLastChangedAt is null ||
        DisplayNameLastChangedAt < DateTime.UtcNow.AddDays(-30);
}
