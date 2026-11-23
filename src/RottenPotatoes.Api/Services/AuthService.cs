using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Security;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

public class AuthService : IAuthService
{
    private readonly RottenPotatoesDbContext _context;
    private readonly JwtSettings _jwt;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(RottenPotatoesDbContext context, IOptions<JwtSettings> jwt)
    {
        _context = context;
        _jwt = jwt.Value;
    }

    /// <summary>
    /// Opretter brugeren med rollen User (registrering giver aldrig Admin). Passwordet gemmes aldrig,
    /// kun et salted PBKDF2-hash. To samtidige registreringer med samme navn afgøres af de unikke
    /// nøgler i Users (DbUpdateException -> null).
    /// </summary>
    public async Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var taken = await _context.Users
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);
        if (taken)
        {
            return null;
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            RoleId = RoleNames.UserRoleId,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return await GetUserAsync(user.Id, cancellationToken);
    }

    /// <summary>
    /// Samme svar (null) for ukendt bruger og forkert password, så man ikke kan gætte brugernavne.
    /// Et vellykket login logges som "Login" i UserActivity.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user is null || !PasswordMatches(user, request.Password))
        {
            return null;
        }

        _context.UserActivities.Add(new UserActivity { UserId = user.Id, ActivityType = "Login" });
        await _context.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes);
        return new AuthResponse
        {
            Token = CreateToken(user, expiresAt),
            ExpiresAt = expiresAt,
            Username = user.Username,
            Role = user.Role.Name,
        };
    }

    /// <summary>Logger "Logout" i UserActivity. Tokenet kan ikke trækkes tilbage; klienten smider det selv væk.</summary>
    public async Task LogoutAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        _context.UserActivities.Add(new UserActivity { UserId = userId, ActivityType = "Logout" });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<UserDto?> GetUserAsync(ulong userId, CancellationToken cancellationToken = default)
        => _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserDto { Id = u.Id, Username = u.Username, Email = u.Email, Role = u.Role.Name })
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Testbrugerne fra Mandatory 1 har "!seed-login-disabled:..." i stedet for et hash; de kan aldrig logge ind.</summary>
    private bool PasswordMatches(User user, string password)
    {
        try
        {
            return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Signeret JWT med brugerens id, navn og rolle. Rollen er en kopi fra databasen på
    /// login-tidspunktet; [Authorize(Roles = ...)] læser den fra "role"-claimet.
    /// </summary>
    private string CreateToken(User user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("role", user.Role.Name),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
    }
}
