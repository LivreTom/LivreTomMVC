using Microsoft.AspNetCore.Identity;

namespace LivreTom.Models;

public class ApplicationUser : IdentityUser
{
    public int Credits { get; set; } = 0;
}