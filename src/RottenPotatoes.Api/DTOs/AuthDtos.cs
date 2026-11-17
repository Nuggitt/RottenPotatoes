using System.ComponentModel.DataAnnotations;

namespace RottenPotatoes.Api.DTOs;

/// <summary>Body til POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Body til POST /api/auth/register. Nye brugere får altid rollen User.</summary>
public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Svar ved login: JWT'en sendes med som "Authorization: Bearer {token}".</summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

/// <summary>Bruger som den vises i API'et (aldrig med PasswordHash).</summary>
public class UserDto
{
    public ulong Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
