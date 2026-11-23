using System.Net;
using System.Net.Mail;

namespace Camstep.Services;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string FromName { get; set; } = "CAMSTEP";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

public class EmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(Microsoft.Extensions.Options.IOptions<SmtpOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("El correo no está configurado (sección Smtp). No se envió: {Subject}", subject);
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.From, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = string.IsNullOrEmpty(_options.User) ? null : new NetworkCredential(_options.User, _options.Password)
            };

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo a {To}", to);
            return false;
        }
    }
}
