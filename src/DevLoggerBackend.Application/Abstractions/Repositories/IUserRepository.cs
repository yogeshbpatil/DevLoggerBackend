using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetDefaultUserAsync(CancellationToken cancellationToken);
}
