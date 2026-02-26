using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Abstractions.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
