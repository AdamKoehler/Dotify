using System.Security.Claims;
using Dotify.Server.Common;
using Dotify.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Dotify.Server.Features.Auth;

public class AuthService(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<DotifyOptions> options) : IAuthService
{
    private static Error InvalidCredentials() =>
        new(ErrorKind.Unauthorized, "invalid_credentials", "Incorrect username or password.");

    private static string? _dummyPasswordHash;

    public async Task<Result<MeDto>> SignupAsync(SignupRequest request, CancellationToken ct)
    {
        if (MissingFields(
                (nameof(SignupRequest.UserName), request.UserName),
                (nameof(SignupRequest.Email), request.Email),
                (nameof(SignupRequest.Password), request.Password)) is { } missing)
        {
            return Result<MeDto>.Fail(missing);
        }

        // Unreserved, a squatted admin name gets granted Admin by the next seed.
        var reservedAdminName = options.Value.Admin.UserName;
        if (!string.IsNullOrWhiteSpace(reservedAdminName) &&
            string.Equals(request.UserName, reservedAdminName, StringComparison.OrdinalIgnoreCase))
        {
            return Result<MeDto>.Fail(
                new Error(ErrorKind.Conflict, "username_taken", "That username is taken."));
        }

        var user = new AppUser { UserName = request.UserName, Email = request.Email };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result<MeDto>.Fail(FromIdentityErrors(created));
        }

        await signIn.SignInAsync(user, isPersistent: true);

        return Result<MeDto>.Ok(await DescribeAsync(user));
    }

    public async Task<Result<MeDto>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (MissingFields(
                (nameof(LoginRequest.UserName), request.UserName),
                (nameof(LoginRequest.Password), request.Password)) is { } missing)
        {
            return Result<MeDto>.Fail(missing);
        }

        var user = await users.FindByNameAsync(request.UserName);
        if (user is null)
        {
            // Do not delete: equalizes PBKDF2 cost so unknown users can't be timed out.
            _dummyPasswordHash ??= passwordHasher.HashPassword(new AppUser(), Guid.NewGuid().ToString());
            passwordHasher.VerifyHashedPassword(new AppUser(), _dummyPasswordHash, request.Password);
            return Result<MeDto>.Fail(InvalidCredentials());
        }

        // lockoutOnFailure: false is deliberate; #40 turns it on with the 423 mapping.
        var result = await signIn.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Result<MeDto>.Fail(InvalidCredentials());
        }

        return Result<MeDto>.Ok(await DescribeAsync(user));
    }

    public Task LogoutAsync(CancellationToken ct) => signIn.SignOutAsync();

    public async Task<Result<MeDto>> GetCurrentUserAsync(
        ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return Result<MeDto>.Fail(
                new Error(ErrorKind.Unauthorized, "not_authenticated", "Not signed in."));
        }

        return Result<MeDto>.Ok(await DescribeAsync(user));
    }

    private async Task<MeDto> DescribeAsync(AppUser user) => new(
        user.UserName!,
        user.Email ?? string.Empty,
        await users.IsInRoleAsync(user, AdminSeeder.AdminRole),
        user.EmailConfirmed);

    private static Error? MissingFields(params (string Field, string? Value)[] members)
    {
        var fields = members
            .Where(m => string.IsNullOrWhiteSpace(m.Value))
            .ToDictionary(m => m.Field, _ => new[] { "This field is required." });

        return fields.Count == 0
            ? null
            : new Error(
                ErrorKind.Validation, "invalid_input", "The submitted values are not valid.", fields);
    }

    private static Error FromIdentityErrors(IdentityResult result)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return new Error(ErrorKind.Conflict, "username_taken", "That username is taken.");
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            return new Error(ErrorKind.Conflict, "email_taken", "That email is already registered.");
        }

        var fields = result.Errors
            .GroupBy(FieldFor)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

        return new Error(
            ErrorKind.Validation, "invalid_input", "The submitted values are not valid.", fields);
    }

    private static string FieldFor(IdentityError error) => error.Code switch
    {
        var c when c.StartsWith("Password", StringComparison.Ordinal) => nameof(SignupRequest.Password),
        var c when c.Contains("UserName", StringComparison.Ordinal) => nameof(SignupRequest.UserName),
        var c when c.Contains("Email", StringComparison.Ordinal) => nameof(SignupRequest.Email),
        _ => "General"
    };
}
