using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Domain.Entities;
using DevLoggerBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevLoggerBackend.Infrastructure.Repositories;

public class DailyLogRepository : IDailyLogRepository
{
    private readonly AppDbContext _dbContext;

    public DailyLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DailyLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DailyLogs
            .AsNoTracking()
            .OrderByDescending(x => x.LogDate)
            .ToListAsync(cancellationToken);
    }

    public Task<DailyLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.DailyLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyLog>> SearchAsync(string? keyword, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken)
    {
        var query = _dbContext.DailyLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();

            query = query.Where(x =>
                x.TasksWorked.ToLower().Contains(normalizedKeyword)
                || x.ProblemsFaced.ToLower().Contains(normalizedKeyword)
                || x.Solutions.ToLower().Contains(normalizedKeyword)
                || x.Learnings.ToLower().Contains(normalizedKeyword)
                || x.Tips.ToLower().Contains(normalizedKeyword)
                || (x.GitLink != null && x.GitLink.ToLower().Contains(normalizedKeyword))
                || x.LogDate.ToString().ToLower().Contains(normalizedKeyword));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.LogDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.LogDate <= dateTo.Value);
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(x => x.LogDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DailyLog dailyLog, CancellationToken cancellationToken)
    {
        await _dbContext.DailyLogs.AddAsync(dailyLog, cancellationToken);
    }

    public void Update(DailyLog dailyLog)
    {
        _dbContext.DailyLogs.Update(dailyLog);
    }

    public void Remove(DailyLog dailyLog)
    {
        _dbContext.DailyLogs.Remove(dailyLog);
    }
}
