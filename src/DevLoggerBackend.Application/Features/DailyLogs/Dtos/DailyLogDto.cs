using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Features.DailyLogs.Dtos;

public class DailyLogDto
{
    public string Id { get; set; } = string.Empty;
    public string LogDate { get; set; } = string.Empty;
    public string TasksWorked { get; set; } = string.Empty;
    public string ProblemsFaced { get; set; } = string.Empty;
    public string Solutions { get; set; } = string.Empty;
    public string Learnings { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string? GitLink { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;

    public static DailyLogDto FromEntity(DailyLog entity)
    {
        return new DailyLogDto
        {
            Id = entity.Id.ToString(),
            LogDate = entity.LogDate.ToString("yyyy-MM-dd"),
            TasksWorked = entity.TasksWorked,
            ProblemsFaced = entity.ProblemsFaced,
            Solutions = entity.Solutions,
            Learnings = entity.Learnings,
            Tips = entity.Tips,
            GitLink = entity.GitLink,
            CreatedAt = entity.CreatedAtUtc.ToString("O"),
            UpdatedAt = entity.UpdatedAtUtc.ToString("O")
        };
    }
}
