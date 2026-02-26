namespace DevLoggerBackend.Application.Features.DailyLogs.Dtos;

public class CreateDailyLogDto
{
    public string LogDate { get; set; } = string.Empty;
    public string TasksWorked { get; set; } = string.Empty;
    public string ProblemsFaced { get; set; } = string.Empty;
    public string Solutions { get; set; } = string.Empty;
    public string Learnings { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string? GitLink { get; set; }
}
