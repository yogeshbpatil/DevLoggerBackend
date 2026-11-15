using DevLoggerBackend.Application.Features.Notes.Commands;
using DevLoggerBackend.Application.Features.Notes.Dtos;
using DevLoggerBackend.Application.Features.Notes.Validators;
using FluentAssertions;

namespace DevLoggerBackend.Application.Tests;

public sealed class SaveNoteCommandValidatorTests
{
    [Fact]
    public void Should_allow_note_content_larger_than_previous_limit()
    {
        var validator = new SaveNoteCommandValidator();
        var command = new SaveNoteCommand(new SaveNoteDto(new string('n', 100_001)));

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_null_note_content()
    {
        var validator = new SaveNoteCommandValidator();
        var command = new SaveNoteCommand(new SaveNoteDto(null!));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
