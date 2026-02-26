using DevLoggerBackend.Application.Features.Auth.Dtos;
using MediatR;

namespace DevLoggerBackend.Application.Features.Auth.Queries;

public sealed record VerifyAuthQuery : IRequest<UserDto?>;

public sealed class VerifyAuthQueryHandler : IRequestHandler<VerifyAuthQuery, UserDto?>
{
    public Task<UserDto?> Handle(VerifyAuthQuery request, CancellationToken cancellationToken)
    {
        // TODO: Use JWT claims to load authenticated user profile.
        return Task.FromResult<UserDto?>(null);
    }
}
