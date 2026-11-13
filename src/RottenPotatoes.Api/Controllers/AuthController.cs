using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;

namespace RottenPotatoes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // TODO: Kald _authService og returnér 200/401.

    [HttpPost("login")]
    public Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpPost("logout")]
    public Task<IActionResult> Logout(CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
