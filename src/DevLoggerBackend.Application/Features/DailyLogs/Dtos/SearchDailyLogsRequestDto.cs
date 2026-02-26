namespace DevLoggerBackend.Application.Features.DailyLogs.Dtos;

public class SearchDailyLogsRequestDto
{
    public string? Keyword { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}
