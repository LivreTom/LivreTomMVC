using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class CreditService(ApplicationDbContext context)
{
    public async Task<bool> ConsumeCreditAsync(string userId, int amount = 1)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Credits < amount)
            return false;

        user.Credits -= amount;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task AddCreditsAsync(string userId, int amount)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.Credits += amount;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUserCreditsAsync(string userId)
    {
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Credits ?? 0;
    }
}
