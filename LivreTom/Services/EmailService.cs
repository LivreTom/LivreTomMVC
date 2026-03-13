using System.Net;
using System.Net.Mail;

namespace LivreTom.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var host = configuration["Email:SmtpHost"]!;
        var port = int.Parse(configuration["Email:SmtpPort"]!);
        var senderEmail = configuration["Email:SenderEmail"]!;
        var senderName = configuration["Email:SenderName"]!;
        var password = configuration["Email:Password"]!;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(senderEmail, password),
            Timeout = 60000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "Redefinição de senha - LivreTom",
            Body = $"""
                <h2>Redefinição de senha</h2>
                <p>Clique no link abaixo para redefinir sua senha:</p>
                <a href="{resetLink}" target="_self">Redefinir minha senha</a>
                <p>O link expira em 1 hora.</p>
                <p>Se você não solicitou isso, ignore este e-mail.</p>
                """,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await client.SendMailAsync(message, cts.Token);
            logger.LogInformation("Email de reset enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar email de reset para {Email}", toEmail);
            throw;
        }
    }
}
