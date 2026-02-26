using DevLoggerBackend.Application.Abstractions.Services;

namespace DevLoggerBackend.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string plainTextPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}
