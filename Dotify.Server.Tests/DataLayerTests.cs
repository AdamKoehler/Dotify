using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dotify.Server.Data;

namespace Dotify.Server.Tests;

public class DataLayerTests : IClassFixture<DotifyApiFactory>
{
    private readonly DotifyApiFactory _factory;
    public DataLayerTests(DotifyApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Track_round_trips_through_database()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DotifyDbContext>();

        var artist = new Artist { Id = Guid.NewGuid(), Name = "Test Artist" };
        var album = new Album { Id = Guid.NewGuid(), Title = "Test Album", ArtistId = artist.Id, Year = 2020 };
        var track = new Track
        {
            Id = Guid.NewGuid(), Title = "Test Track", AlbumId = album.Id, ArtistId = artist.Id,
            TrackNumber = 1, DurationSeconds = 180, FilePath = "audio/x.mp3",
            ContentType = "audio/mpeg", FileSizeBytes = 1234, AudioHash = "abc", UploadedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(artist, album, track);
        await db.SaveChangesAsync();

        var loaded = await db.Tracks.Include(t => t.Album).ThenInclude(a => a!.Artist)
            .SingleAsync(t => t.Id == track.Id);
        Assert.Equal("Test Track", loaded.Title);
        Assert.Equal("Test Album", loaded.Album!.Title);
        Assert.Equal("Test Artist", loaded.Album.Artist!.Name);
    }
}
