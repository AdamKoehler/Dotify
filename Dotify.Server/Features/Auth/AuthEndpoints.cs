using System.Security.Claims;
using Dotify.Server.Common;
using Microsoft.AspNetCore.Antiforgery;

namespace Dotify.Server.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapGet("/csrf", (HttpContext http, IAntiforgery antiforgery, IWebHostEnvironment env) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);

            http.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = !env.IsDevelopment() || http.Request.IsHttps
                });

            return Results.Ok(new CsrfTokenDto(tokens.RequestToken!));
        })
        .AllowAnonymous()
        .WithName("CsrfToken");

        auth.MapPost("/signup", async (
                SignupRequest request, IAuthService service, CancellationToken ct) =>
            (await service.SignupAsync(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .AllowAnonymous()
            .WithName("Signup");

        auth.MapPost("/login", async (
                LoginRequest request, IAuthService service, CancellationToken ct) =>
            (await service.LoginAsync(request, ct)).ToHttpResult())
            .AllowAnonymous()
            .WithName("Login");

        auth.MapPost("/logout", async (IAuthService service, CancellationToken ct) =>
        {
            await service.LogoutAsync(ct);
            return Results.NoContent();
        }).WithName("Logout");

        auth.MapGet("/me", async (
                ClaimsPrincipal principal, IAuthService service, CancellationToken ct) =>
            (await service.GetCurrentUserAsync(principal, ct)).ToHttpResult())
            .WithName("Me");

        return api;
    }
}
