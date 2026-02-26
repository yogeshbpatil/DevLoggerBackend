using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Commands;

public sealed record UpdateDailyLogCommand(Guid Id, CreateDailyLogDto Payload) : IRequest<DailyLogDto>;

public sealed class UpdateDailyLogCommandHandler : IRequestHandler<UpdateDailyLogCommand, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDailyLogCommandHandler(IDailyLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DailyLogDto> Handle(UpdateDailyLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Daily log with id '{request.Id}' not found.");
        }

        if (!DateOnly.TryParse(request.Payload.LogDate, out var parsedDate))
        {
            throw new FluentValidation.ValidationException("logDate must be in YYYY-MM-DD format.");
        }

        entity.LogDate = parsedDate;
        entity.TasksWorked = request.Payload.TasksWorked;
        entity.ProblemsFaced = request.Payload.ProblemsFaced;
        entity.Solutions = request.Payload.Solutions;
        entity.Learnings = request.Payload.Learnings;
        entity.Tips = request.Payload.Tips;
        entity.GitLink = string.IsNullOrWhiteSpace(request.Payload.GitLink) ? null : request.Payload.GitLink;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DailyLogDto.FromEntity(entity);
    }
}
