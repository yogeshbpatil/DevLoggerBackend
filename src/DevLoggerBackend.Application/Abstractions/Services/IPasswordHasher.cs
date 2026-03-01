namespace DevLoggerBackend.Application.Abstractions.Services;

public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string passwordHash);
}
