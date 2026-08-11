using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotify.Server.Data;

// Lets `dotnet ef migrations add` build the model without a live connection string —
// Aspire only injects ConnectionStrings:dotify at runtime.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DotifyDbContext>
{
    public DotifyDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DotifyDbContext>().UseSqlServer().Options);
}
