using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class MusicService(ApplicationDbContext context, CreditService creditService)
{
    public async Task<List<MusicOrder>> GetOrdersByUserAsync(string userId)
    {
        return await context.MusicOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateOrderAsync(string userId, string category, Dictionary<string, string> userAnswers)
    {
        var creditConsumed = await creditService.ConsumeCreditAsync(userId);
        if (!creditConsumed)
            return (false, "Você não possui créditos suficientes.");

        var order = new MusicOrder
        {
            UserId = userId,
            Status = "Processando",
            CreatedAt = DateTime.Now,
            CreditsSpent = 1,
            FinalPrompt = $"Geração de {category} baseada em {userAnswers.Count} respostas."
        };

        context.MusicOrders.Add(order);
        await context.SaveChangesAsync();
        foreach (var answer in userAnswers)
        {
            var userAnswer = new UserAnswer
            {
                MusicOrderId = order.Id,
                QuestionKey = answer.Key,
                Answer = answer.Value
            };
            context.UserAnswers.Add(userAnswer);
        }

        await context.SaveChangesAsync();

        return (true, "Pedido enviado com sucesso!");
    }
}
