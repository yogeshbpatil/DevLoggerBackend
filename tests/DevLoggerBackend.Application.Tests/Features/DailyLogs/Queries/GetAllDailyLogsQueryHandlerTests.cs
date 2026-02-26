using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Queries;
using DevLoggerBackend.Domain.Entities;
using FluentAssertions;
using Moq;

namespace DevLoggerBackend.Application.Tests.Features.DailyLogs.Queries;

public class GetAllDailyLogsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnLogsSortedFromRepositoryResult()
    {
        var logs = new List<DailyLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                LogDate = new DateOnly(2026, 2, 20),
                TasksWorked = "A",
                ProblemsFaced = "P",
                Solutions = "S",
                Learnings = "L",
                Tips = "T",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        var repository = new Mock<IDailyLogRepository>();
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(logs);

        var handler = new GetAllDailyLogsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllDailyLogsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].TasksWorked.Should().Be("A");
    }
}
