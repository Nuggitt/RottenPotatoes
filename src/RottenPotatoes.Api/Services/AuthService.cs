using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;

namespace RottenPotatoes.Api.Services;

public class AuthService : IAuthService
{
    private readonly RottenPotatoesDbContext _context;

    public AuthService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    // TODO: Slå brugeren op i Users og verificér password mod PasswordHash.
    // TODO: Roller (fx admin og normal bruger) til authorization.

    public Task LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: login.");

    public Task LogoutAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: logout.");
}
