using DevLoggerBackend.Application.Features.Notes.Commands;
using FluentValidation;

namespace DevLoggerBackend.Application.Features.Notes.Validators;

public sealed class SaveNoteCommandValidator : AbstractValidator<SaveNoteCommand>
{
    public SaveNoteCommandValidator()
    {
        RuleFor(x => x.Payload.Content).NotNull().MaximumLength(100_000);
    }
}
