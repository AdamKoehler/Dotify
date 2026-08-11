using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dotify.Server.Data;

public class DotifyDbContext(DbContextOptions<DotifyDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Artist>().HasIndex(a => a.Name).IsUnique();
        b.Entity<Album>().HasIndex(a => new { a.ArtistId, a.Title }).IsUnique();
        b.Entity<Track>().HasIndex(t => t.AudioHash).IsUnique();
        // SQL Server forbids multiple cascade paths: Artist -> Track exists both directly and
        // via Album. Keep the Album chain cascading; the direct FK must not cascade.
        b.Entity<Track>()
            .HasOne(t => t.Artist).WithMany().HasForeignKey(t => t.ArtistId)
            .OnDelete(DeleteBehavior.NoAction);
        b.Entity<PlaylistTrack>().HasKey(pt => new { pt.PlaylistId, pt.TrackId });
        b.Entity<PlaylistTrack>()
            .HasOne<Playlist>().WithMany(p => p.Tracks).HasForeignKey(pt => pt.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Favorite>().HasKey(f => new { f.UserId, f.TrackId });
    }
}
