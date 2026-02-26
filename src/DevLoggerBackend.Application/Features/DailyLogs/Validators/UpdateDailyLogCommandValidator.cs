using DevLoggerBackend.Application.Features.DailyLogs.Commands;
using FluentValidation;

namespace DevLoggerBackend.Application.Features.DailyLogs.Validators;

public class UpdateDailyLogCommandValidator : AbstractValidator<UpdateDailyLogCommand>
{
    public UpdateDailyLogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Payload.LogDate)
            .NotEmpty()
            .Must(BeValidDate).WithMessage("logDate must be a valid date in YYYY-MM-DD format.")
            .Must(NotBeFutureDate).WithMessage("logDate must not be in the future.");

        RuleFor(x => x.Payload.TasksWorked).NotEmpty();
        RuleFor(x => x.Payload.ProblemsFaced).NotEmpty();
        RuleFor(x => x.Payload.Solutions).NotEmpty();
        RuleFor(x => x.Payload.Learnings).NotEmpty();

        RuleFor(x => x.Payload.GitLink)
            .Must(BeValidOptionalUrl)
            .WithMessage("gitLink must be a valid URL when provided.");
    }

    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);

    private static bool NotBeFutureDate(string value)
    {
        if (!DateOnly.TryParse(value, out var parsed)) return false;
        return parsed <= DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static bool BeValidOptionalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
