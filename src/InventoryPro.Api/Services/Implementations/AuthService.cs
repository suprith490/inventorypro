using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Mapping;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = NormalizeEmail(request.Email);

        var emailAlreadyUsed = await _context.Users
            .AnyAsync(user => user.Email == email);

        if (emailAlreadyUsed)
        {
            throw new ConflictException($"An account with email '{email}' already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Staff,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == email);

        // Same error for "unknown email" and "wrong password" so attackers
        // cannot discover which accounts exist.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated. Contact an administrator.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId);

        if (user is null)
        {
            throw new NotFoundException($"User with id {userId} was not found.");
        }

        return user.ToProfileDto();
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = user.ToProfileDto()
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
