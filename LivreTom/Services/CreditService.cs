using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class CreditService
{
    private readonly ApplicationDbContext _context;

    public CreditService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Verifica se o usuário tem saldo e debita 1 crédito
    public async Task<bool> ConsumeCreditAsync(string userId, int amount = 1)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Credits < amount)
        {
            return false; // Saldo insuficiente ou usuário não encontrado
        }

        user.Credits -= amount;
        await _context.SaveChangesAsync();
        return true;
    }

    // Adiciona créditos (útil para o cadastro inicial ou compras)
    public async Task AddCreditsAsync(string userId, int amount)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.Credits += amount;
            await _context.SaveChangesAsync();
        }
    }

    // Retorna o saldo atual
    public async Task<int> GetUserCreditsAsync(string userId)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        return user?.Credits ?? 0;
    }
}