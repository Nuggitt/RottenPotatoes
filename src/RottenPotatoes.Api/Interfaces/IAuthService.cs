using RottenPotatoes.Api.DTOs;

namespace RottenPotatoes.Api.Interfaces;

/// <summary>Login/logout. Returtyperne afhænger af, om I vælger cookie- eller token-baseret login.</summary>
public interface IAuthService
{
    // TODO: Skift returtype, når login-mekanismen er valgt (fx et token eller en ClaimsPrincipal).
    Task LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
