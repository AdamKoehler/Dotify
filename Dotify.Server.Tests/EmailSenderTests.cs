using Dotify.Server.Features.Auth;
using Microsoft.Extensions.Logging;

namespace Dotify.Server.Tests;

public class EmailSenderTests
{
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private const string Link =
        "http://localhost:5173/confirm-email?userId=user-1&token=CfDJ8Ab%2Bc%2Fd%3D";

    [Fact]
    public async Task Logging_sender_writes_recipient_subject_and_body()
    {
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger);

        await sender.SendAsync(
            "listener@example.com", "Confirm your email", $"Confirm here: {Link}", default);

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("listener@example.com", entry.Message);
        Assert.Contains("Confirm your email", entry.Message);
        Assert.Contains(Link, entry.Message);
    }

    // The dev flow is "read the link out of the Aspire dashboard", which Debug/Trace would hide.
    [Fact]
    public async Task Logging_sender_logs_at_information_level()
    {
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger);

        await sender.SendAsync("listener@example.com", "Subject", "Body", default);

        Assert.Equal(LogLevel.Information, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task Logging_sender_is_an_IEmailSender()
    {
        IEmailSender sender = new LoggingEmailSender(new CapturingLogger<LoggingEmailSender>());

        await sender.SendAsync("listener@example.com", "Subject", "Body", default);
    }
}
