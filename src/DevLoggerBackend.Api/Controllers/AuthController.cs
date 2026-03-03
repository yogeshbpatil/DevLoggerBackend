using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.Auth.Commands;
using DevLoggerBackend.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    [HttpGet("verify")]
    public async Task<IActionResult> Verify(
    [FromServices] IUserRepository userRepository,
    [FromServices] ICurrentUserService currentUserService,
    CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("User no longer exists.");

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role.ToString()
        });
    }
}
