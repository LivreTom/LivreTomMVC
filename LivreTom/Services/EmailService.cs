using System.Net;
using System.Net.Mail;

namespace LivreTom.Services;

public class EmailService(IConfiguration configuration)
{
    private readonly IConfiguration _config = configuration;

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var senderEmail = _config["Email:SenderEmail"]!;
        var senderName = _config["Email:SenderName"]!;
        var password = _config["Email:Password"]!;

        var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(senderEmail, password)
        };

        var message = new MailMessage
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

        await client.SendMailAsync(message);
    }
}
