using System.Net;
using System.Net.Mail;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpOptions, ILogger<SmtpEmailSender> logger)
    {
        _smtpSettings = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
        {
            _logger.LogInformation(
                "Email sender is in log-only mode. To={To} Subject={Subject} Body={Body}",
                toEmail,
                subject,
                body);
            return;
        }

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
            Subject = subject,
            Body = body
        };

        message.To.Add(toEmail);

        using var reg = cancellationToken.Register(() => client.SendAsyncCancel());
        await client.SendMailAsync(message);
    }
}
