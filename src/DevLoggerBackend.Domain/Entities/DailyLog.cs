using DevLoggerBackend.Domain.Common;

namespace DevLoggerBackend.Domain.Entities;

public class DailyLog : BaseEntity
{
    public DateOnly LogDate { get; set; }
    public string TasksWorked { get; set; } = string.Empty;
    public string ProblemsFaced { get; set; } = string.Empty;
    public string Solutions { get; set; } = string.Empty;
    public string Learnings { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string? GitLink { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
