using System.Security.Claims;
using RbacWebApi.Models;

namespace RbacWebApi.Services;

public interface IJwtTokenService
{
    (string token, DateTime expiresAt) GenerateToken(SysUser user, List<SysRole> roles);
    ClaimsPrincipal? ValidateToken(string token);
    string? GetUserIdFromClaims(ClaimsPrincipal principal);
    List<string> GetRolesFromClaims(ClaimsPrincipal principal);
}
