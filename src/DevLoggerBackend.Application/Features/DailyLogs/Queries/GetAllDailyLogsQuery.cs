using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using MediatR;


namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record GetAllDailyLogsQuery : IRequest<IReadOnlyList<DailyLogDto>>;

public sealed class GetAllDailyLogsQueryHandler : IRequestHandler<GetAllDailyLogsQuery, IReadOnlyList<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllDailyLogsQueryHandler(
    IDailyLogRepository repository,
    ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DailyLogDto>> Handle(
    GetAllDailyLogsQuery request,
    CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var logs = await _repository.GetByUserIdAsync(userId, cancellationToken);

        return logs.Select(DailyLogDto.FromEntity).ToList();
    }
}
