using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Features.DailyLogs.Commands;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Domain.Entities;
using DevLoggerBackend.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevLoggerBackend.Application.Tests.Features.DailyLogs.Commands;

public class CreateDailyLogCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateDailyLog_WhenPayloadIsValid()
    {
        var repository = new Mock<IDailyLogRepository>();
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        userRepository.Setup(x => x.GetDefaultUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Email = "john@example.com",
                PasswordHash = "hash",
                Role = UserRole.Developer
            });

        var handler = new CreateDailyLogCommandHandler(repository.Object, userRepository.Object, unitOfWork.Object);

        var dto = new CreateDailyLogDto
        {
            LogDate = "2026-02-20",
            TasksWorked = "Implemented API",
            ProblemsFaced = "None",
            Solutions = "N/A",
            Learnings = "CQRS",
            Tips = "Ship small increments",
            GitLink = "https://github.com/example/repo"
        };

        var result = await handler.Handle(new CreateDailyLogCommand(dto), CancellationToken.None);

        result.TasksWorked.Should().Be("Implemented API");
        repository.Verify(x => x.AddAsync(It.IsAny<DailyLog>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
