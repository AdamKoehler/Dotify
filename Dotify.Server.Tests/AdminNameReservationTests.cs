using System.Net;
using System.Net.Http.Json;
using Dotify.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Dotify.Server.Tests;

public sealed class UnseededAdminApiFactory : DotifyApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Dotify:Admin:Password", "");
    }
}

public class AdminNameReservationTests : IClassFixture<UnseededAdminApiFactory>
{
    private readonly UnseededAdminApiFactory _factory;
    public AdminNameReservationTests(UnseededAdminApiFactory factory) => _factory = factory;

    [Fact]
    public async Task The_admin_account_really_is_absent_in_this_host()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        Assert.Null(await users.FindByNameAsync("admin"));
    }

    [Fact]
    public async Task Signup_cannot_claim_the_configured_admin_username()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = "admin", email = "squatter@example.com", password = "Password1!" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("username_taken", body!["code"].ToString());
    }

    [Fact]
    public async Task The_reservation_is_case_insensitive()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = "ADMIN", email = "squatter-upper@example.com", password = "Password1!" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("username_taken", body!["code"].ToString());
    }
}
