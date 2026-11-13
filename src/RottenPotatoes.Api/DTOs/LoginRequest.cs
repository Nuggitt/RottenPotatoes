namespace RottenPotatoes.Api.DTOs;

/// <summary>Body til POST /api/auth/login. Matcher Users.Username / Users.PasswordHash.</summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
