using LivreTom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace LivreTom.Controllers;

[Route("api/stripe")]
public class StripeController(
    IConfiguration configuration,
    CreditService creditService,
    ILogger<StripeController> logger) : Controller
{
    // Planos com desconto: tokens → preço total em centavos
    private static readonly Dictionary<int, long> Plans = new()
    {
        { 3,  8490 },   // R$ 84,90  → R$ 28,30/token → 6% off
        { 5,  12990 },  // R$ 129,90 → R$ 25,98/token → 13% off
        { 10, 22990 },  // R$ 229,90 → R$ 22,99/token → 23% off
        { 20, 42990 },  // R$ 429,90 → R$ 21,50/token → 28% off
        { 30, 59990 },  // R$ 599,90 → R$ 20,00/token → 33% off
    };

    private const long FullPricePerTokenCents = 3000; // R$ 30,00

    // Rastreia sessões já processadas para evitar crédito duplicado (webhook + redirect)
    // Em produção com múltiplas instâncias, substituir por tabela no banco de dados
    private static readonly ConcurrentDictionary<string, byte> ProcessedSessions = new();

    [HttpGet("checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckout(
        [FromQuery] int plan,
        [FromQuery] bool custom = false,
        [FromQuery] string? returnUrl = null)
    {
        if (plan < 1 || plan > 500)
            return BadRequest("Quantidade inválida.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Sanitizar returnUrl para evitar open-redirect
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
            returnUrl = "/";

        // Plano com desconto ou preço cheio para quantidade personalizada
        long totalCents;
        if (!custom && Plans.TryGetValue(plan, out var discountedPrice))
            totalCents = discountedPrice;
        else
            totalCents = plan * FullPricePerTokenCents;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var options = new SessionCreateOptions
        {
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        UnitAmount = totalCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{plan} Token{(plan > 1 ? "s" : "")} de Música",
                            Description = $"Pacote de {plan} token{(plan > 1 ? "s" : "")} para produção musical no LivreTom",
                        },
                    },
                    Quantity = 1,
                },
            ],
            Mode = "payment",
            PaymentMethodTypes = ["card"],
            SuccessUrl = $"{baseUrl}/api/stripe/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}{returnUrl}?payment=cancelled",
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId },
                { "tokens", plan.ToString() },
                { "returnUrl", returnUrl },
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Redirect(session.Url);
    }

    /// <summary>
    /// Chamado quando o Stripe redireciona após pagamento bem-sucedido.
    /// Verifica a sessão diretamente na API do Stripe e credita os tokens.
    /// </summary>
    [HttpGet("success")]
    public async Task<IActionResult> ConfirmPayment([FromQuery(Name = "session_id")] string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return Redirect("/?payment=cancelled");

        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            var returnUrl = session.Metadata?.GetValueOrDefault("returnUrl") ?? "/";

            if (session.PaymentStatus == "paid"
                && session.Metadata is not null
                && session.Metadata.TryGetValue("userId", out var userId)
                && session.Metadata.TryGetValue("tokens", out var tokensStr)
                && int.TryParse(tokensStr, out var tokens))
            {
                // Só credita se essa sessão ainda não foi processada (pelo webhook ou outro redirect)
                if (ProcessedSessions.TryAdd(sessionId, 0))
                {
                    await creditService.AddCreditsAsync(userId, tokens);
                    logger.LogInformation("✅ {Tokens} tokens creditados via redirect para usuário {UserId}", tokens, userId);
                }
                else
                {
                    logger.LogInformation("ℹ️ Sessão {SessionId} já processada, ignorando duplicata", sessionId);
                }
            }

            return Redirect($"{returnUrl}?payment=success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro ao confirmar sessão {SessionId}", sessionId);
            return Redirect("/?payment=cancelled");
        }
    }

    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session is not null
                    && session.Metadata is not null
                    && session.Metadata.TryGetValue("userId", out var userId)
                    && session.Metadata.TryGetValue("tokens", out var tokensStr)
                    && int.TryParse(tokensStr, out var tokens))
                {
                    // Só credita se essa sessão ainda não foi processada (pelo redirect ou outro webhook)
                    if (ProcessedSessions.TryAdd(session.Id, 0))
                    {
                        await creditService.AddCreditsAsync(userId, tokens);
                        logger.LogInformation("✅ {Tokens} tokens creditados via webhook para usuário {UserId}", tokens, userId);
                    }
                    else
                    {
                        logger.LogInformation("ℹ️ Sessão {SessionId} já processada via redirect, webhook ignorado", session.Id);
                    }
                }
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "❌ Erro ao processar webhook do Stripe");
            return BadRequest();
        }
    }
}