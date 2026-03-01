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
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RegisterCommand(request.Name, request.Email, request.Password, request.ConfirmPassword),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { message = "User registered successfully." });
    }

    /// <summary>
    /// Authenticates a registered user and returns a token.
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
