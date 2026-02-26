namespace DevLoggerBackend.Application.Features.Auth.Dtos;

public class LoginResponseDto
{
    public UserDto User { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}
