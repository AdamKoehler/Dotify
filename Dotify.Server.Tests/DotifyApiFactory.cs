using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Dotify.Server.Tests;

public sealed class DotifyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();
    public string DataRoot { get; } =
        Path.Combine(Path.GetTempPath(), "dotify-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync() => await _sql.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(DataRoot);
        var conn = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "dotify"
        };
        builder.UseSetting("ConnectionStrings:dotify", conn.ConnectionString);
        builder.UseSetting("Dotify:DataRoot", DataRoot);
        builder.UseSetting("Dotify:Admin:UserName", "admin");
        builder.UseSetting("Dotify:Admin:Password", "AdminPass1!");
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        try { Directory.Delete(DataRoot, recursive: true); } catch { }
    }
}
