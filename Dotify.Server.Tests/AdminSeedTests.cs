using Dotify.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Dotify.Server.Tests;

public class AdminSeedTests : IClassFixture<DotifyApiFactory>
{
    private readonly DotifyApiFactory _factory;
    public AdminSeedTests(DotifyApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_user_is_seeded_into_the_Admin_role()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var admin = await users.FindByNameAsync("admin");

        Assert.NotNull(admin);
        Assert.True(await users.IsInRoleAsync(admin, "Admin"));
        Assert.Equal("admin@dotify.local", admin.Email);
        Assert.True(admin.EmailConfirmed);
    }

    [Fact]
    public async Task Seeding_is_idempotent()
    {
        using var scope = _factory.Services.CreateScope();

        await Dotify.Server.Features.Auth.AdminSeeder.SeedAsync(scope.ServiceProvider);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Single(users.Users.Where(u => u.UserName == "admin"));
    }

    [Fact]
    public async Task Seeded_admin_password_is_the_configured_one()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var admin = await users.FindByNameAsync("admin");

        Assert.NotNull(admin);
        Assert.True(await users.CheckPasswordAsync(admin, "AdminPass1!"));
    }
}
