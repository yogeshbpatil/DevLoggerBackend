using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Domain.Entities;
using DevLoggerBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevLoggerBackend.Infrastructure.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly AppDbContext _dbContext;

    public NoteRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Note?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Notes.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(Note note, CancellationToken cancellationToken) =>
        await _dbContext.Notes.AddAsync(note, cancellationToken);
}
