using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class MusicService
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;

    public MusicService(ApplicationDbContext context, CreditService creditService)
    {
        _context = context;
        _creditService = creditService;
    }

    public async Task<(bool Success, string Message)> CreateOrderAsync(string userId, string category, Dictionary<string, string> userAnswers)
    {
        // 1. Verificar e Consumir Crédito
        var creditConsumed = await _creditService.ConsumeCreditAsync(userId);
        if (!creditConsumed)
        {
            return (false, "Você não possui créditos suficientes.");
        }

        // 2. Criar o Pedido Principal
        var order = new MusicOrder
        {
            UserId = userId,
            Status = "Processando",
            CreatedAt = DateTime.Now,
            CreditsSpent = 1,
            // Aqui você poderia montar um "Prompt Provisório" combinando as respostas
            FinalPrompt = $"Geração de {category} baseada em {userAnswers.Count} respostas."
        };

        _context.MusicOrders.Add(order);
        await _context.SaveChangesAsync(); // Salva para gerar o ID do pedido

        // 3. Salvar as Respostas Detalhadas
        foreach (var answer in userAnswers)
        {
            var userAnswer = new UserAnswer
            {
                MusicOrderId = order.Id,
                QuestionKey = answer.Key,
                Answer = answer.Value
            };
            _context.UserAnswers.Add(userAnswer);
        }

        await _context.SaveChangesAsync();

        return (true, "Pedido enviado com sucesso! Sua música está sendo composta.");
    }
}