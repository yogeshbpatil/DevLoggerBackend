using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Domain.Entities;
using MediatR;
using DevLoggerBackend.Application.Abstractions.Services;

namespace DevLoggerBackend.Application.Features.DailyLogs.Commands;

public sealed record CreateDailyLogCommand(CreateDailyLogDto Payload) : IRequest<DailyLogDto>;

public sealed class CreateDailyLogCommandHandler : IRequestHandler<CreateDailyLogCommand, DailyLogDto>
{
    private readonly IDailyLogRepository _dailyLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateDailyLogCommandHandler(IDailyLogRepository dailyLogRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dailyLogRepository = dailyLogRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService; ;
    }

    public async Task<DailyLogDto> Handle(CreateDailyLogCommand request, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(request.Payload.LogDate, out var parsedDate))
        {
            throw new FluentValidation.ValidationException("logDate must be in YYYY-MM-DD format.");
        }

        

        var entity = new DailyLog
        {
            Id = Guid.NewGuid(),
            LogDate = parsedDate,
            TasksWorked = request.Payload.TasksWorked,
            ProblemsFaced = request.Payload.ProblemsFaced,
            Solutions = request.Payload.Solutions,
            Learnings = request.Payload.Learnings,
            Tips = request.Payload.Tips,
            GitLink = string.IsNullOrWhiteSpace(request.Payload.GitLink) ? null : request.Payload.GitLink,
            UserId = _currentUserService.UserId
    ?? throw new UnauthorizedException("User is not authenticated."),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _dailyLogRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DailyLogDto.FromEntity(entity);
    }
}
