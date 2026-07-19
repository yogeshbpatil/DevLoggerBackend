using DevLoggerBackend.Domain.Common;

namespace DevLoggerBackend.Domain.Entities;

public class Note : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
