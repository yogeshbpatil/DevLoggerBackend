using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Application.Abstractions.Services;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record GetDailyLogByIdQuery(Guid Id) : IRequest<DailyLogDto>;

public sealed class GetDailyLogByIdQueryHandler : IRequestHandler<GetDailyLogByIdQuery, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetDailyLogByIdQueryHandler(
    IDailyLogRepository repository,
    ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<DailyLogDto> Handle(GetDailyLogByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var log = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (log is null || log.UserId != userId)
        {
            throw new NotFoundException($"Daily log with id '{request.Id}' not found.");
        }

        return DailyLogDto.FromEntity(log);
    }
}
