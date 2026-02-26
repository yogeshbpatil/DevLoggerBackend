namespace DevLoggerBackend.Application.Abstractions.Services;

public interface IPasswordHasher
{
    bool Verify(string plainTextPassword, string passwordHash);
}
