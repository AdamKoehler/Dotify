namespace Dotify.Server.Features.Auth;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string plainTextBody, CancellationToken ct)
    {
        logger.LogInformation(
            "Email to {Recipient} — {Subject}{NewLine}{Body}",
            to,
            subject,
            Environment.NewLine,
            plainTextBody);

        return Task.CompletedTask;
    }
}
