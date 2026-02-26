using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Domain.Entities;
using DevLoggerBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevLoggerBackend.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLower();
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalized, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetDefaultUserAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Users.OrderBy(x => x.Email).FirstOrDefaultAsync(cancellationToken);
    }
}
