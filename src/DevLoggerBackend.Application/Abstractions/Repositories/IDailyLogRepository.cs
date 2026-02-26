using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Abstractions.Repositories;

public interface IDailyLogRepository
{
    Task<IReadOnlyList<DailyLog>> GetAllAsync(CancellationToken cancellationToken);
    Task<DailyLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DailyLog>> SearchAsync(string? keyword, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken);
    Task AddAsync(DailyLog dailyLog, CancellationToken cancellationToken);
    void Update(DailyLog dailyLog);
    void Remove(DailyLog dailyLog);
}
