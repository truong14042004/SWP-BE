using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public Task SendOtpAsync(
        string toEmail,
        string fullName,
        string otp,
        CancellationToken cancellationToken)
    {
        return SendInternalAsync(
            toEmail,
            "CareerMap email verification OTP",
            BuildOtpBody(fullName, otp),
            isHtml: false,
            cancellationToken);
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        // Auto-detect HTML body via simple heuristic.
        var isHtml = body.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || body.Contains("<body", StringComparison.OrdinalIgnoreCase)
            || body.Contains("<p>", StringComparison.OrdinalIgnoreCase)
            || body.Contains("<div", StringComparison.OrdinalIgnoreCase);

        return SendInternalAsync(toEmail, subject, body, isHtml, cancellationToken);
    }

    private async Task SendInternalAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("SMTP configuration is missing.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildOtpBody(string fullName, string otp) =>
        $"""
        Hello {fullName},

        Your CareerMap verification OTP is: {otp}

        This code expires in 10 minutes. If you did not request this, ignore this email.

        CareerMap
        """;
}
