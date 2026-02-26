using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Infrastructure.Services;

public class PlaceholderTokenService : ITokenService
{
    public string GenerateToken(User user)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"devlogger:{user.Email}:{DateTime.UtcNow:O}"));
    }
}
