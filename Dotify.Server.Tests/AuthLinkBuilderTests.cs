using Dotify.Server.Data;
using Dotify.Server.Features.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Dotify.Server.Tests;

public class AuthLinkBuilderTests
{
    // Shaped like an Identity data-protection token: base64, including + / and =.
    private const string TrickyToken = "CfDJ8Ab+c/d=eF+gh/i==";

    private static AuthLinkBuilder For(string? publicBaseUrl) =>
        new(Options.Create(new DotifyOptions { PublicBaseUrl = publicBaseUrl }));

    private static string QueryValue(string url, string key) =>
        QueryHelpers.ParseQuery(new Uri(url).Query)[key]!;

    [Fact]
    public void ConfirmEmail_points_at_the_spa_confirm_email_page()
    {
        var url = For("http://localhost:5173").ConfirmEmail("user-1", "tok");

        Assert.StartsWith("http://localhost:5173/confirm-email?", url);
    }

    [Fact]
    public void ResetPassword_points_at_the_spa_reset_password_page()
    {
        var url = For("http://localhost:5173").ResetPassword("user-1", "tok");

        Assert.StartsWith("http://localhost:5173/reset-password?", url);
    }

    [Fact]
    public void Both_links_carry_the_userId_and_token()
    {
        var url = For("http://localhost:5173").ConfirmEmail("user-42", "tok-1");

        Assert.Equal("user-42", QueryValue(url, "userId"));
        Assert.Equal("tok-1", QueryValue(url, "token"));
    }

    [Fact]
    public void Token_containing_plus_slash_and_equals_round_trips_byte_identical()
    {
        var url = For("http://localhost:5173").ConfirmEmail("user-1", TrickyToken);

        Assert.Equal(TrickyToken, QueryValue(url, "token"));
    }

    [Fact]
    public void Reset_token_round_trips_byte_identical_too()
    {
        var url = For("http://localhost:5173").ResetPassword("user-1", TrickyToken);

        Assert.Equal(TrickyToken, QueryValue(url, "token"));
    }

    [Fact]
    public void Raw_token_characters_are_escaped_in_the_url()
    {
        var url = For("http://localhost:5173").ConfirmEmail("user-1", TrickyToken);

        Assert.DoesNotContain(TrickyToken, url);
        Assert.DoesNotContain("+", url);
        Assert.Contains("%2B", url);
    }

    [Fact]
    public void UserId_is_escaped_as_well()
    {
        var url = For("http://localhost:5173").ConfirmEmail("user id/1", "tok");

        Assert.Equal("user id/1", QueryValue(url, "userId"));
    }

    [Theory]
    [InlineData("http://localhost:5173/app")]
    [InlineData("http://localhost:5173/app/")]
    public void A_base_url_with_a_path_prefix_keeps_the_prefix(string baseUrl)
    {
        var url = For(baseUrl).ConfirmEmail("user-1", "tok");

        Assert.StartsWith("http://localhost:5173/app/confirm-email?", url);
    }

    [Fact]
    public void A_base_url_at_the_root_does_not_double_its_slash()
    {
        var url = For("https://dotify.example.com/").ConfirmEmail("user-1", "tok");

        Assert.StartsWith("https://dotify.example.com/confirm-email?", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/confirm-email")]
    [InlineData("localhost:5173")]
    [InlineData("ftp://dotify.example.com/app")]
    public void An_absent_relative_or_non_http_base_url_is_rejected(string? publicBaseUrl)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => For(publicBaseUrl));

        Assert.Equal(AuthLinkBuilder.PublicBaseUrlError, ex.Message);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("/relative", false)]
    [InlineData("localhost:5173", false)]
    [InlineData("ftp://dotify.example.com/app", false)]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://dotify.example.com/app", true)]
    public void IsValidPublicBaseUrl_matches_what_the_constructor_accepts(string? value, bool expected)
    {
        Assert.Equal(expected, AuthLinkBuilder.IsValidPublicBaseUrl(value));
    }
}
