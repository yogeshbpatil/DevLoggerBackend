using DevLoggerBackend.Domain.Common;
using DevLoggerBackend.Domain.Enums;

namespace DevLoggerBackend.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public ICollection<DailyLog> DailyLogs { get; set; } = new List<DailyLog>();
}
