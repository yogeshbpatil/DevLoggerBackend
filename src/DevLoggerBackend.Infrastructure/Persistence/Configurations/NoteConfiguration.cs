using DevLoggerBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevLoggerBackend.Infrastructure.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(100_000);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User)
            .WithOne(x => x.Note)
            .HasForeignKey<Note>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
