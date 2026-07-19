using DevLoggerBackend.Domain.Entities;

namespace DevLoggerBackend.Application.Features.Notes.Dtos;

public sealed record NoteDto(Guid Id, string Content, DateTime UpdatedAtUtc)
{
    public static NoteDto FromEntity(Note note) =>
        new(note.Id, note.Content, note.UpdatedAtUtc);
}

public sealed record SaveNoteDto(string Content);
