using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Abstractions.Repositories;

public interface INoteRepository
{
    Task<Note?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Note note, CancellationToken cancellationToken);
}
