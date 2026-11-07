using System.Net;
using System.Net.Http.Json;

namespace Dotify.Server.Tests;

public class CsrfTokenTests : IClassFixture<DotifyApiFactory>
{
    private readonly DotifyApiFactory _factory;
    public CsrfTokenTests(DotifyApiFactory factory) => _factory = factory;

    private record CsrfResponse(string Token);

    [Fact]
    public async Task Csrf_endpoint_returns_a_token_matching_the_XSRF_TOKEN_cookie()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/auth/csrf");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));

        var xsrf = resp.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        Assert.Contains($"XSRF-TOKEN={body.Token}", xsrf);
    }

    [Fact]
    public async Task Csrf_endpoint_sets_a_javascript_readable_XSRF_TOKEN_cookie()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/auth/csrf");

        var setCookies = resp.Headers.GetValues("Set-Cookie").ToList();
        var xsrf = Assert.Single(setCookies, c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));

        Assert.DoesNotContain("httponly", xsrf, StringComparison.OrdinalIgnoreCase);
    }
}
