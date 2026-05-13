namespace SWP_BE.Services;

public interface IEmailSender
{
    Task SendOtpAsync(string toEmail, string fullName, string otp, CancellationToken cancellationToken);
}
