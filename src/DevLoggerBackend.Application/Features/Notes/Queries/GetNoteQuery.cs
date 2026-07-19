using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.Notes.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.Notes.Queries;

public sealed record GetNoteQuery : IRequest<NoteDto?>;

public sealed class GetNoteQueryHandler : IRequestHandler<GetNoteQuery, NoteDto?>
{
    private readonly INoteRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetNoteQueryHandler(INoteRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<NoteDto?> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");
        var note = await _repository.GetByUserIdAsync(userId, cancellationToken);
        return note is null ? null : NoteDto.FromEntity(note);
    }
}
