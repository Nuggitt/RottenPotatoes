using RottenPotatoes.Api.DTOs;

namespace RottenPotatoes.Api.Interfaces;

/// <summary>Registrering, login (JWT) og logout.</summary>
public interface IAuthService
{
    /// <summary>Opretter en bruger med rollen User. Null, hvis brugernavn eller email er i brug.</summary>
    Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>JWT ved korrekt brugernavn og password, ellers null. Logger "Login" i UserActivity.</summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Logger "Logout" i UserActivity. Selve tokenet smides væk af klienten.</summary>
    Task LogoutAsync(ulong userId, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserAsync(ulong userId, CancellationToken cancellationToken = default);
}
