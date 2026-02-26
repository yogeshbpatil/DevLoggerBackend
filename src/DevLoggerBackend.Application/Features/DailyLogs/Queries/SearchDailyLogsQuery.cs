using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record SearchDailyLogsQuery(string? Keyword, string? DateFrom, string? DateTo) : IRequest<IReadOnlyList<DailyLogDto>>;

public sealed class SearchDailyLogsQueryHandler : IRequestHandler<SearchDailyLogsQuery, IReadOnlyList<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;

    public SearchDailyLogsQueryHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DailyLogDto>> Handle(SearchDailyLogsQuery request, CancellationToken cancellationToken)
    {
        DateOnly? dateFrom = ParseDate(request.DateFrom);
        DateOnly? dateTo = ParseDate(request.DateTo);

        var logs = await _repository.SearchAsync(request.Keyword, dateFrom, dateTo, cancellationToken);
        return logs.Select(DailyLogDto.FromEntity).ToList();
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParse(value, out var parsed) ? parsed : null;
    }
}
