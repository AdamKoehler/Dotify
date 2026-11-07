namespace Dotify.Server.Features.Auth;

public record SignupRequest(string UserName, string Email, string Password);

public record LoginRequest(string UserName, string Password);

public record MeDto(string UserName, string Email, bool IsAdmin, bool EmailConfirmed);

public record CsrfTokenDto(string Token);
