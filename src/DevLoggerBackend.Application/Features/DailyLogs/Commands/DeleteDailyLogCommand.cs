using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Common.Exceptions;
using MediatR;

namespace DevLoggerBackend.Application.Features.DailyLogs.Commands;

public sealed record DeleteDailyLogCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteDailyLogCommandHandler : IRequestHandler<DeleteDailyLogCommand, Unit>
{
    private readonly IDailyLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDailyLogCommandHandler(IDailyLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteDailyLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Daily log with id '{request.Id}' not found.");
        }

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
