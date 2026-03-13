using Resend;

namespace LivreTom.Services;

public class EmailService(IResend resend, IConfiguration configuration)
{
    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var senderEmail = configuration["Resend:SenderEmail"]!;
        var senderName = configuration["Resend:SenderName"]!;

        var message = new EmailMessage
        {
            From = $"{senderName} <{senderEmail}>",
            Subject = "Redefinição de senha - LivreTom",
            HtmlBody = $"""
                <h2>Redefinição de senha</h2>
                <p>Clique no link abaixo para redefinir sua senha:</p>
                <a href="{resetLink}">Redefinir minha senha</a>
                <p>O link expira em 1 hora.</p>
                <p>Se você não solicitou isso, ignore este e-mail.</p>
                """
        };
        message.To.Add(toEmail);

        await resend.EmailSendAsync(message);
    }
}