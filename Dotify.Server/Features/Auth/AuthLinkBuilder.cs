using Dotify.Server.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Dotify.Server.Features.Auth;

public sealed class AuthLinkBuilder(IOptions<DotifyOptions> options)
{
    public const string PublicBaseUrlError =
        "Dotify:PublicBaseUrl must be set to an absolute URL (the SPA's public origin).";

    private readonly Uri _baseUri = ResolveBaseUri(options.Value.PublicBaseUrl);

    public string ConfirmEmail(string userId, string token) =>
        Build("confirm-email", userId, token);

    public string ResetPassword(string userId, string token) =>
        Build("reset-password", userId, token);

    // Keep the scheme check: Uri.TryCreate("localhost:5173", Absolute) returns true, parsing
    // "localhost" as the scheme and "5173" as the path.
    public static bool IsValidPublicBaseUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static Uri ResolveBaseUri(string? publicBaseUrl)
    {
        if (!IsValidPublicBaseUrl(publicBaseUrl))
        {
            throw new InvalidOperationException(PublicBaseUrlError);
        }

        // Without a trailing slash, Uri's relative resolution drops the last segment:
        // "http://host/app" + "confirm-email" yields "http://host/confirm-email".
        var builder = new UriBuilder(publicBaseUrl!) { Query = null, Fragment = null };
        if (!builder.Path.EndsWith('/'))
        {
            builder.Path += "/";
        }

        return builder.Uri;
    }

    private string Build(string page, string userId, string token) =>
        QueryHelpers.AddQueryString(
            new Uri(_baseUri, page).ToString(),
            new Dictionary<string, string?>
            {
                ["userId"] = userId,
                ["token"] = token
            });
}
