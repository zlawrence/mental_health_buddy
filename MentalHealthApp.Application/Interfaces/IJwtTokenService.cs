using System.Security.Claims;

namespace MentalHealthApp.Application.Services;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string role, string email);
    ClaimsPrincipal? ValidateToken(string token);
}
