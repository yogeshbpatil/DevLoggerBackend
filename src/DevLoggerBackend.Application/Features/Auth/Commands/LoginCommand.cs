using DevLoggerBackend.Application.Abstractions.Repositories;
using DevLoggerBackend.Application.Abstractions.Services;
using DevLoggerBackend.Application.Common.Exceptions;
using DevLoggerBackend.Application.Features.Auth.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return new LoginResponseDto
        {
            User = new UserDto
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Email = user.Email,
                Role = user.Role switch
                {
                    Domain.Enums.UserRole.SeniorDeveloper => "Senior Developer",
                    Domain.Enums.UserRole.TeamLead => "Team Lead",
                    _ => user.Role.ToString()
                }
            },
            Token = _tokenService.GenerateToken(user)
        };
    }
}
