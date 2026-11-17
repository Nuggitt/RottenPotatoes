using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Security;

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

    /// <summary>Opretter en ny bruger med rollen User.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await _authService.RegisterAsync(request, cancellationToken);

        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Brugernavn eller email er allerede i brug.");
        }

        return CreatedAtAction(nameof(Me), null, user);
    }

    /// <summary>Logger ind og returnerer en JWT. Brug den i Swagger via "Authorize".</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);

        if (response is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Forkert brugernavn eller password.");
        }

        return Ok(response);
    }

    /// <summary>Logger "Logout". Klienten skal selv smide tokenet væk (JWT kan ikke trækkes tilbage).</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(User.GetUserId(), cancellationToken);

        return NoContent();
    }

    /// <summary>Den indloggede bruger (praktisk til at se, hvem tokenet tilhører).</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetUserAsync(User.GetUserId(), cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
