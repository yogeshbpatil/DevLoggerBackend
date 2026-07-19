using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.Notes.Dtos;
using DevLoggerBackend.Domain.Entities;
using MediatR;

namespace DevLoggerBackend.Application.Features.Notes.Commands;

public sealed record SaveNoteCommand(SaveNoteDto Payload) : IRequest<NoteDto>;

public sealed class SaveNoteCommandHandler : IRequestHandler<SaveNoteCommand, NoteDto>
{
    private readonly INoteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveNoteCommandHandler(INoteRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<NoteDto> Handle(SaveNoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");
        var note = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (note is null)
        {
            note = new Note { UserId = userId, Content = request.Payload.Content };
            await _repository.AddAsync(note, cancellationToken);
        }
        else
        {
            note.Content = request.Payload.Content;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoteDto.FromEntity(note);
    }
}
