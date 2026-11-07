using Dotify.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Dotify.Server.Features.Auth;

public static class AdminSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminSeeder));

        var options = services.GetRequiredService<IOptions<DotifyOptions>>().Value.Admin;
        if (string.IsNullOrWhiteSpace(options.UserName) ||
            string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning(
                "Admin seed skipped: Dotify:Admin:UserName and/or Dotify:Admin:Password is not " +
                "configured, so NO admin account will exist and nothing will hold the {Role} " +
                "role. Set both (locally: the AppHost's 'admin-password' parameter) and restart.",
                AdminRole);
            return;
        }

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole))
        {
            var roleCreated = await roles.CreateAsync(new IdentityRole(AdminRole));
            if (!roleCreated.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Admin role seed failed: {Describe(roleCreated)}");
            }
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        var admin = await users.FindByNameAsync(options.UserName);
        var adopted = admin is not null;

        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = options.UserName,
                Email = options.ResolveEmail(),
                EmailConfirmed = true
            };

            var created = await users.CreateAsync(admin, options.Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"Admin seed failed: {Describe(created)}");
            }
        }

        var alreadyInRole = await users.IsInRoleAsync(admin, AdminRole);
        if (!alreadyInRole)
        {
            var added = await users.AddToRoleAsync(admin, AdminRole);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Admin role assignment failed: {Describe(added)}");
            }
        }

        if (adopted && !alreadyInRole)
        {
            logger.LogWarning(
                "Admin seed granted the {Role} role to pre-existing account {UserName}, which it " +
                "did not create. The configured Dotify:Admin:Password was NOT applied to that " +
                "account. If you did not create it yourself, treat it as compromised.",
                AdminRole, options.UserName);
        }
        else if (adopted)
        {
            logger.LogInformation(
                "Admin seed found pre-existing account {UserName} already in the {Role} role. It " +
                "was not created by this run, so the configured Dotify:Admin:Password was NOT " +
                "applied to it.",
                options.UserName, AdminRole);
        }
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
