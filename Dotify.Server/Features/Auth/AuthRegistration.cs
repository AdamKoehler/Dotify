using Dotify.Server.Common;
using Dotify.Server.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Dotify.Server.Features.Auth;

public static class AuthRegistration
{
    public static IHostApplicationBuilder AddDotifyAuth(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.AddIdentityCore<AppUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DotifyDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddHttpContextAccessor();

        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "dotify_auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;

            // SameAsRequest in Development: the test host is plain HTTP and would drop a Secure cookie.
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;

            o.Events.OnRedirectToLogin = ctx =>
                WriteProblemAsync(
                    ctx.HttpContext,
                    ErrorKind.Unauthorized,
                    "not_authenticated",
                    "Not signed in.");
            o.Events.OnRedirectToAccessDenied = ctx =>
                WriteProblemAsync(
                    ctx.HttpContext,
                    ErrorKind.Forbidden,
                    "forbidden",
                    "You do not have permission to perform this action.");
        });

        builder.Services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-XSRF-TOKEN";

            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        builder.Services.AddAuthorization(o =>
            o.AddPolicy("Admin", p => p.RequireRole(AdminSeeder.AdminRole)));

        builder.Services.AddScoped<IAuthService, AuthService>();

        return builder;
    }

    private static Task WriteProblemAsync(
        HttpContext httpContext, ErrorKind kind, string code, string detail) =>
        new Error(kind, code, detail).ToHttpResult().ExecuteAsync(httpContext);
}
