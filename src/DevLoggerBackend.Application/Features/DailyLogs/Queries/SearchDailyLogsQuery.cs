using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Queries;

public sealed record SearchDailyLogsQuery(string? Keyword, string? DateFrom, string? DateTo) : IRequest<IReadOnlyList<DailyLogDto>>;

public sealed class SearchDailyLogsQueryHandler : IRequestHandler<SearchDailyLogsQuery, IReadOnlyList<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public SearchDailyLogsQueryHandler(IDailyLogRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DailyLogDto>> Handle(
     SearchDailyLogsQuery request,
     CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        DateOnly? dateFrom = ParseDate(request.DateFrom);
        DateOnly? dateTo = ParseDate(request.DateTo);

        var logs = await _repository.SearchByUserIdAsync(
    userId,
    request.Keyword,
    dateFrom,
    dateTo,
    cancellationToken);

        return logs.Select(DailyLogDto.FromEntity).ToList();
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParse(value, out var parsed) ? parsed : null;
    }
}
