using Microsoft.AspNetCore.Identity;

namespace Dotify.Server.Data;

public class AppUser : IdentityUser { }

public class Artist
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public List<Album> Albums { get; set; } = [];
}

public class Album
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public Guid ArtistId { get; set; }
    public Artist? Artist { get; set; }
    public int? Year { get; set; }
    public string? CoverPath { get; set; }
    public List<Track> Tracks { get; set; } = [];
}

public class Track
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public Guid AlbumId { get; set; }
    public Album? Album { get; set; }
    public Guid ArtistId { get; set; }
    public Artist? Artist { get; set; }
    public int? TrackNumber { get; set; }
    public int DurationSeconds { get; set; }
    public required string FilePath { get; set; }        // relative to data root, e.g. "audio/{Id}.mp3"
    public required string ContentType { get; set; }     // "audio/mpeg" | "audio/mp4" | "audio/aac"
    public long FileSizeBytes { get; set; }
    public required string AudioHash { get; set; }       // SHA-256 hex of file bytes, dedupe key
    public DateTimeOffset UploadedAt { get; set; }
}

public class Playlist
{
    public Guid Id { get; set; }
    public required string OwnerUserId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PlaylistTrack> Tracks { get; set; } = [];
}

public class PlaylistTrack
{
    public Guid PlaylistId { get; set; }
    public Guid TrackId { get; set; }
    public Track? Track { get; set; }
    public int Position { get; set; }
}

public class Favorite
{
    public required string UserId { get; set; }
    public Guid TrackId { get; set; }
    public Track? Track { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
