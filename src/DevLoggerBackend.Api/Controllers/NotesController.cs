using DevLoggerBackend.Application.Features.Notes.Commands;
using DevLoggerBackend.Application.Features.Notes.Dtos;
using DevLoggerBackend.Application.Features.Notes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevLoggerBackend.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<NoteDto>> Get(CancellationToken cancellationToken)
    {
        var note = await _mediator.Send(new GetNoteQuery(), cancellationToken);
        return note is null ? NoContent() : Ok(note);
    }

    /// <summary>Creates the current user's note, or updates it when it already exists.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NoteDto>> Save([FromBody] SaveNoteDto request, CancellationToken cancellationToken)
    {
        var note = await _mediator.Send(new SaveNoteCommand(request), cancellationToken);
        return Ok(note);
    }
}
