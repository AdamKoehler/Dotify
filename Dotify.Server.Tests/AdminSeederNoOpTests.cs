using Dotify.Server.Data;
using Dotify.Server.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Dotify.Server.Tests;

public class AdminSeederNoOpTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("admin", null)]
    [InlineData(null, "AdminPass1!")]
    [InlineData("admin", "   ")]
    public async Task Seeding_no_ops_when_the_admin_is_not_fully_configured(
        string? userName, string? password)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .Configure<DotifyOptions>(o =>
            {
                o.Admin.UserName = userName;
                o.Admin.Password = password;
            })
            .BuildServiceProvider();

        var ex = await Record.ExceptionAsync(() => AdminSeeder.SeedAsync(services));

        Assert.Null(ex);
    }
}
