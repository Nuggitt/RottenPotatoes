namespace RottenPotatoes.Api.Security;

/// <summary>
/// Konfiguration af JWT (sektionen "Jwt" i appsettings.json).
/// Key sættes via miljøvariablen Jwt__Key fra JWT_SECRET i .env – aldrig i kildekoden.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Hemmelig nøgle til at signere tokens (HMAC-SHA256). Mindst 32 tegn.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;
}
