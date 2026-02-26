using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record GetAllDailyLogsQuery : IRequest<IReadOnlyList<DailyLogDto>>;

public sealed class GetAllDailyLogsQueryHandler : IRequestHandler<GetAllDailyLogsQuery, IReadOnlyList<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;

    public GetAllDailyLogsQueryHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DailyLogDto>> Handle(GetAllDailyLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _repository.GetAllAsync(cancellationToken);
        return logs.Select(DailyLogDto.FromEntity).ToList();
    }
}
