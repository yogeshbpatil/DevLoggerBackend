using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record GetDailyLogByIdQuery(Guid Id) : IRequest<DailyLogDto>;

public sealed class GetDailyLogByIdQueryHandler : IRequestHandler<GetDailyLogByIdQuery, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;

    public GetDailyLogByIdQueryHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyLogDto> Handle(GetDailyLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (log is null)
        {
            throw new NotFoundException($"Daily log with id '{request.Id}' not found.");
        }

        return DailyLogDto.FromEntity(log);
    }
}
