using System.Security.Claims;
using Dotify.Server.Common;

namespace Dotify.Server.Features.Auth;

public interface IAuthService
{
    Task<Result<MeDto>> SignupAsync(SignupRequest request, CancellationToken ct);
    Task<Result<MeDto>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task LogoutAsync(CancellationToken ct);
    Task<Result<MeDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken ct);
}
