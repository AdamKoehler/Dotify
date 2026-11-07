using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Dotify.Server.Tests;

public class AuthTests : IClassFixture<DotifyApiFactory>
{
    private readonly DotifyApiFactory _factory;
    public AuthTests(DotifyApiFactory factory) => _factory = factory;

    private static string UniqueName() => "user" + Guid.NewGuid().ToString("N")[..12];

    private record MeResponse(string UserName, string Email, bool IsAdmin, bool EmailConfirmed);

    [Fact]
    public async Task Me_without_login_returns_problem_details_not_a_redirect()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal(401, ((JsonElement)body!["status"]).GetInt32());
        Assert.Equal("not_authenticated", body["code"].ToString());
    }

    [Fact]
    public async Task Signup_then_me_roundtrip()
    {
        var client = _factory.CreateClient();
        var name = UniqueName();

        var signup = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"{name}@example.com", password = "Password1!" });
        Assert.Equal(HttpStatusCode.Created, signup.StatusCode);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(name, me!.UserName);
        Assert.Equal($"{name}@example.com", me.Email);
        Assert.False(me.IsAdmin);
        Assert.False(me.EmailConfirmed);
    }

    [Fact]
    public async Task Signup_then_login_then_me_roundtrip()
    {
        var name = UniqueName();

        var signupClient = _factory.CreateClient();
        var signup = await signupClient.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"{name}@example.com", password = "Password1!" });
        Assert.Equal(HttpStatusCode.Created, signup.StatusCode);

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = name, password = "Password1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(name, me!.UserName);
        Assert.False(me.IsAdmin);
    }

    [Fact]
    public async Task Seeded_admin_can_login_and_is_admin()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "admin", password = "AdminPass1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.True(me!.IsAdmin);
        Assert.True(me.EmailConfirmed);
    }

    [Fact]
    public async Task Login_with_bad_password_returns_401_with_invalid_credentials_code()
    {
        var client = _factory.CreateClient();
        var name = UniqueName();
        var signup = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"{name}@example.com", password = "Password1!" });
        Assert.Equal(HttpStatusCode.Created, signup.StatusCode);

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { userName = name, password = "WrongPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("invalid_credentials", body!["code"].ToString());
    }

    [Fact]
    public async Task Login_with_unknown_user_is_indistinguishable_from_a_bad_password()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = UniqueName(), password = "Password1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("invalid_credentials", body!["code"].ToString());
    }

    [Fact]
    public async Task Signup_with_a_taken_username_returns_409_username_taken()
    {
        var client = _factory.CreateClient();
        var name = UniqueName();
        var first = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"{name}@example.com", password = "Password1!" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _factory.CreateClient().PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"other-{name}@example.com", password = "Password1!" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("username_taken", body!["code"].ToString());
    }

    [Fact]
    public async Task Signup_with_a_taken_email_returns_409_email_taken()
    {
        var client = _factory.CreateClient();
        var name = UniqueName();
        var email = $"{name}@example.com";
        var first = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email, password = "Password1!" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _factory.CreateClient().PostAsJsonAsync("/api/auth/signup",
            new { userName = UniqueName(), email, password = "Password1!" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("email_taken", body!["code"].ToString());
    }

    [Fact]
    public async Task Signup_with_a_weak_password_returns_400_with_field_errors()
    {
        var client = _factory.CreateClient();
        var name = UniqueName();

        var resp = await client.PostAsJsonAsync("/api/auth/signup",
            new { userName = name, email = $"{name}@example.com", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("invalid_input", body!["code"].ToString());

        var errors = Assert.IsType<JsonElement>(body["errors"]);
        var passwordErrors = errors.GetProperty("Password");
        Assert.Equal(JsonValueKind.Array, passwordErrors.ValueKind);
        Assert.True(passwordErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Logout_clears_the_session()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "admin", password = "AdminPass1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/signup", "{\"userName\":\"somebody\",\"email\":\"somebody@example.com\"}", "Password")]
    [InlineData("/api/auth/login", "{\"password\":\"Password1!\"}", "UserName")]
    [InlineData("/api/auth/login", "{\"userName\":\"nobody\"}", "Password")]
    public async Task Signup_or_login_without_a_required_field_returns_400_invalid_input(
        string endpoint, string jsonBody, string missingField)
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync(
            endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("invalid_input", body!["code"].ToString());
        var errors = Assert.IsType<JsonElement>(body["errors"]);
        Assert.True(errors.GetProperty(missingField).GetArrayLength() > 0);
    }

    [Fact]
    public async Task Me_with_an_html_accept_header_returns_401_not_500()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/html");

        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("not_authenticated", body!["code"].ToString());
    }

    [Fact]
    public async Task Login_sets_a_hardened_identity_cookie()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "admin", password = "AdminPass1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var cookie = Assert.Single(
            login.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("dotify_auth=", StringComparison.Ordinal));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
