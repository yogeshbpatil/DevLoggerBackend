using DevLoggerBackend.Application.Features.Auth.Commands;
using DevLoggerBackend.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevLoggerBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates user with seeded credentials.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// JWT-based verification scaffold.
    /// </summary>
    [HttpGet("verify")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Verify(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "TODO: Enable JWT authentication and return authenticated user payload."
        });
    }
}
