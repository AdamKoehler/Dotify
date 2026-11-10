using Dotify.Server.Features.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace Dotify.Server.Tests;

public class DotifyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();
    public string DataRoot { get; } =
        Path.Combine(Path.GetTempPath(), "dotify-tests", Guid.NewGuid().ToString("N"));
    public FakeEmailSender Emails { get; } = new();

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
        builder.UseSetting("Dotify:Admin:Email", "admin@dotify.local");
        builder.UseSetting("Dotify:Admin:Password", "AdminPass1!");
        builder.UseSetting("Dotify:PublicBaseUrl", "http://localhost:5173");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        try { Directory.Delete(DataRoot, recursive: true); } catch { }
    }
}
