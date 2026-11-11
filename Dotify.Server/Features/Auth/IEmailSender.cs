namespace Dotify.Server.Features.Auth;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string plainTextBody, CancellationToken ct);
}
