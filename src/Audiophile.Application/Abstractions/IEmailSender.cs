namespace Audiophile.Application.Abstractions;

// Sends one email, Mailpit or any SMTP server locally and Brevo's HTTP API in production, chosen by the Email settings
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string Html, string Text);
