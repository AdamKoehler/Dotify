using Dotify.Server.Features.Auth;

namespace Dotify.Server.Tests;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<SentEmail> _sent = [];
    private readonly object _gate = new();

    public IReadOnlyList<SentEmail> Sent
    {
        get { lock (_gate) { return [.. _sent]; } }
    }

    public Task SendAsync(string to, string subject, string plainTextBody, CancellationToken ct)
    {
        lock (_gate)
        {
            _sent.Add(new SentEmail(to, subject, plainTextBody));
        }

        return Task.CompletedTask;
    }

    // Look mail up by recipient, never by "most recent": DotifyApiFactory is an IClassFixture, so
    // every test in a class shares this instance.
    public SentEmail LastTo(string recipient)
    {
        lock (_gate)
        {
            for (var i = _sent.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_sent[i].To, recipient, StringComparison.OrdinalIgnoreCase))
                {
                    return _sent[i];
                }
            }
        }

        throw new InvalidOperationException($"No email was sent to '{recipient}'.");
    }

    public bool AnyTo(string recipient)
    {
        lock (_gate)
        {
            return _sent.Exists(
                e => string.Equals(e.To, recipient, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _sent.Clear();
        }
    }
}
