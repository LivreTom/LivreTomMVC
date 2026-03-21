using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class MusicService(ApplicationDbContext context, CreditService creditService)
{
    public async Task<MusicOrder?> GetOrderByIdAsync(int orderId)
        => await context.MusicOrders.FindAsync(orderId);

    public async Task<List<MusicOrder>> GetOrdersByUserAsync(string userId)
    {
        await DeleteExpiredOrdersAsync(userId);

        return await context.MusicOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retorna todos os pedidos com dados do usuário (para painel admin).
    /// </summary>
    public async Task<List<MusicOrder>> GetAllOrdersAsync()
    {
        return await context.MusicOrders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateOrderAsync(string userId, string category, Dictionary<string, string> userAnswers)
    {
        var creditConsumed = await creditService.ConsumeCreditAsync(userId);
        if (!creditConsumed)
            return (false, "Você não possui tokens de música suficientes.");

        var order = new MusicOrder
        {
            UserId = userId,
            Status = "Pendente",
            CreatedAt = DateTime.UtcNow,
            CreditsSpent = 1,
            FinalPrompt = $"Geração de {category} baseada em {userAnswers.Count} respostas."
        };

        context.MusicOrders.Add(order);
        await context.SaveChangesAsync();

        foreach (var answer in userAnswers)
        {
            context.UserAnswers.Add(new UserAnswer
            {
                MusicOrderId = order.Id,
                QuestionKey = answer.Key,
                Answer = answer.Value
            });
        }

        await context.SaveChangesAsync();
        return (true, "Pedido enviado com sucesso!");
    }

    /// <summary>
    /// Chamado pelo admin para concluir um pedido com o link curto do Suno.
    /// Resolve o SunoSongId, monta a CDN URL e marca como Concluído.
    /// </summary>
    public async Task<(bool Success, string Message)> FulfillOrderAsync(int orderId, string sunoShortUrl, string? title = null, string? lyrics = null)
    {
        var order = await context.MusicOrders.FindAsync(orderId);
        if (order is null)
            return (false, "Pedido não encontrado.");

        var songId = await ResolveSunoShortLinkAsync(sunoShortUrl);
        if (string.IsNullOrEmpty(songId))
            return (false, "Não foi possível resolver o link do Suno.");

        order.SunoSongId = songId;
        order.CoverImageUrl = $"https://cdn1.suno.ai/image_{songId}.jpeg";
        order.Status = "Concluído";

        if (!string.IsNullOrEmpty(title))
            order.Title = title;

        if (!string.IsNullOrEmpty(lyrics))
            order.Lyrics = lyrics;

        await context.SaveChangesAsync();
        return (true, $"Pedido #{orderId} concluído com sucesso!");
    }

    /// <summary>
    /// Marca que o usuário aceitou o termo ao baixar (primeiro download).
    /// </summary>
    public async Task<(bool Success, string Message)> ConfirmDownloadAsync(int orderId, string userId)
    {
        var order = await context.MusicOrders.FindAsync(orderId);
        if (order is null || order.UserId != userId)
            return (false, "Pedido não encontrado.");

        if (order.DownloadConfirmed)
            return (true, "Download já confirmado.");

        order.DownloadConfirmed = true;
        order.DownloadConfirmedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Download confirmado.");
    }

    /// <summary>
    /// Resolve um link curto do Suno (ex: https://suno.com/s/xxxxx) para o SongId.
    /// </summary>
    public static async Task<string?> ResolveSunoShortLinkAsync(string shortUrl)
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);

            var response = await client.GetAsync(shortUrl);
            var location = response.Headers.Location?.ToString();

            if (!string.IsNullOrEmpty(location) && location.Contains("/song/"))
                return location.Split("/song/").Last().Split("?").First();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task DeleteExpiredOrdersAsync(string userId)
    {
        var expiredOrders = await context.MusicOrders
            .Where(o => o.UserId == userId && o.CreatedAt < DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        if (expiredOrders.Count != 0)
        {
            context.MusicOrders.RemoveRange(expiredOrders);
            await context.SaveChangesAsync();
        }
    }
}
