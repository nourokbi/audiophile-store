using System.Net.Http.Json;
using Audiophile.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Audiophile.Infrastructure.Email;

// Sends through SMTP, Mailpit catches everything locally so no real person gets a test email
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var o = options.Value;
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody(),
        };
        mime.From.Add(new MailboxAddress(o.FromName, o.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));

        using var client = new SmtpClient();
        await client.ConnectAsync(o.Smtp.Host, o.Smtp.Port, o.Smtp.UseTls ? SecureSocketOptions.Auto : SecureSocketOptions.None, ct);
        if (!string.IsNullOrEmpty(o.Smtp.Username))
            await client.AuthenticateAsync(o.Smtp.Username, o.Smtp.Password, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }
}

// Sends through Brevo's HTTP API, since free hosts such as Render block the SMTP ports
internal sealed class BrevoEmailSender(HttpClient http, IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var o = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = o.FromName, email = o.FromAddress },
                to = new[] { new { email = message.ToAddress, name = message.ToName } },
                subject = message.Subject,
                htmlContent = message.Html,
                textContent = message.Text,
            }),
        };
        request.Headers.Add("api-key", o.BrevoApiKey);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Brevo refused the email with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
    }
}

// Used when no provider is set, writes the subject to the log and sends nothing
internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("No email provider is set, so \"{Subject}\" was not sent", message.Subject);
        return Task.CompletedTask;
    }
}
