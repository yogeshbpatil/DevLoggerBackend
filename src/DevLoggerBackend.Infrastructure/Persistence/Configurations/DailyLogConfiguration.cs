using DevLoggerBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevLoggerBackend.Infrastructure.Persistence.Configurations;

public class DailyLogConfiguration : IEntityTypeConfiguration<DailyLog>
{
    public void Configure(EntityTypeBuilder<DailyLog> builder)
    {
        builder.ToTable("DailyLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TasksWorked).IsRequired();
        builder.Property(x => x.ProblemsFaced).IsRequired();
        builder.Property(x => x.Solutions).IsRequired();
        builder.Property(x => x.Learnings).IsRequired();
        builder.Property(x => x.Tips).HasDefaultValue(string.Empty);
        builder.Property(x => x.GitLink).HasMaxLength(2048);

        builder.HasOne(x => x.User)
            .WithMany(x => x.DailyLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LogDate);
    }
}
