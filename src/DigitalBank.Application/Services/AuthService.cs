using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using DigitalBank.Domain.Entities;

namespace DigitalBank.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IApplicationUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByUsernameAsync(dto.Username, ct))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        var user = new ApplicationUser
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = "Employee"
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var (token, expiresAtUtc) = _tokenService.GenerateToken(user);
        return new AuthResponseDto(token, expiresAtUtc, user.Username, user.Role);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username, ct);

        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var (token, expiresAtUtc) = _tokenService.GenerateToken(user);
        return new AuthResponseDto(token, expiresAtUtc, user.Username, user.Role);
    }
}
