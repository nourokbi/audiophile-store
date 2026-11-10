namespace Audiophile.Infrastructure.Email;

// Provider is Log (write to the log only), Smtp (Mailpit locally or any SMTP server) or Brevo (its HTTP API, for hosts that block SMTP)
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Log";
    public string FromAddress { get; set; } = "orders@audiophile.local";
    public string FromName { get; set; } = "Audiophile";
    public SmtpSettings Smtp { get; set; } = new();
    public string BrevoApiKey { get; set; } = "";

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;

        // Encrypts the connection, off only for Mailpit on the same machine or Docker network
        public bool UseTls { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
