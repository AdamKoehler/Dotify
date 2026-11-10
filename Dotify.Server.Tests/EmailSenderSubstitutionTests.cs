using Dotify.Server.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Dotify.Server.Tests;

public class EmailSenderSubstitutionTests : IClassFixture<DotifyApiFactory>
{
    private readonly DotifyApiFactory _factory;
    public EmailSenderSubstitutionTests(DotifyApiFactory factory) => _factory = factory;

    [Fact]
    public void The_test_host_resolves_the_factorys_fake_sender()
    {
        using var scope = _factory.Services.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        Assert.Same(_factory.Emails, resolved);
    }

    [Fact]
    public async Task Mail_sent_through_the_resolved_sender_is_captured_by_the_factory()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var recipient = $"capture-{Guid.NewGuid():N}@example.com";

        await sender.SendAsync(recipient, "Confirm your email", "Confirm here: http://x/y", default);

        var captured = _factory.Emails.LastTo(recipient);
        Assert.Equal("Confirm your email", captured.Subject);
        Assert.Contains("http://x/y", captured.Body);
    }

    [Fact]
    public async Task LastTo_matches_the_recipient_case_insensitively()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var local = $"mixed-{Guid.NewGuid():N}";

        await sender.SendAsync($"{local}@Example.COM", "Subject", "Body", default);

        Assert.Equal("Subject", _factory.Emails.LastTo($"{local}@example.com").Subject);
    }

    [Fact]
    public void LastTo_throws_a_named_error_when_no_mail_reached_the_recipient()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => _factory.Emails.LastTo("nobody@example.com"));

        Assert.Contains("nobody@example.com", ex.Message);
    }

    [Fact]
    public async Task LastTo_returns_the_most_recent_message_for_that_recipient()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var recipient = $"repeat-{Guid.NewGuid():N}@example.com";

        await sender.SendAsync(recipient, "First", "Body", default);
        await sender.SendAsync(recipient, "Second", "Body", default);

        Assert.Equal("Second", _factory.Emails.LastTo(recipient).Subject);
    }
}
