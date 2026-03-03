using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Abstractions.Services;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Commands;

public sealed record DeleteDailyLogCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteDailyLogCommandHandler : IRequestHandler<DeleteDailyLogCommand, Unit>
{
    private readonly IDailyLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDailyLogCommandHandler(IDailyLogRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteDailyLogCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (entity is null || entity.UserId != userId)
        {
            throw new NotFoundException($"Daily log with id '{request.Id}' not found.");
        }

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
