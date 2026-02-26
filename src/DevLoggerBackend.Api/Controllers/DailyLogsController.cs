using DevLoggerBackend.Application.Features.DailyLogs.Commands;
using DevLoggerBackend.Application.Features.DailyLogs.Dtos;
using DevLoggerBackend.Application.Features.DailyLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevLoggerBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DailyLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all daily logs sorted by logDate descending.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyLogDto>>> GetAll(CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new GetAllDailyLogsQuery(), cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Returns a daily log by id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DailyLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyLogDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new GetDailyLogByIdQuery(id), cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Creates a daily log.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DailyLogDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DailyLogDto>> Create([FromBody] CreateDailyLogDto request, CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new CreateDailyLogCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
    }

    /// <summary>
    /// Updates a daily log.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DailyLogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyLogDto>> Update(Guid id, [FromBody] CreateDailyLogDto request, CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new UpdateDailyLogCommand(id, request), cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Deletes a daily log.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDailyLogCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Searches logs by keyword and optional date range.
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyLogDto>>> Search([FromBody] SearchDailyLogsRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new SearchDailyLogsQuery(request.Keyword, request.DateFrom, request.DateTo), cancellationToken);
        return Ok(data);
    }
}
