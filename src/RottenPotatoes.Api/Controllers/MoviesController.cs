using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Security;

namespace RottenPotatoes.Api.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    // GET er åbent for alle. POST, PUT og DELETE kræver rollen Admin (401 uden token, 403 med forkert rolle).

    /// <summary>Henter film med pagination, filtrering og sortering.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MovieDto>>> GetMovies([FromQuery] MovieQuery query, CancellationToken cancellationToken)
    {
        var result = await _movieService.GetMoviesAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>Henter én film ud fra id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieDto>> GetMovie(uint id, CancellationToken cancellationToken)
    {
        var movie = await _movieService.GetMovieByIdAsync(id, cancellationToken);

        if (movie is null)
        {
            return NotFound();
        }

        return Ok(movie);
    }

    /// <summary>Opretter en film.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovieDto>> CreateMovie(CreateMovieDto dto, CancellationToken cancellationToken)
    {
        var movie = await _movieService.CreateMovieAsync(dto, cancellationToken);

        // 201 Created + Location-header, der peger på GET /api/movies/{id}.
        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
    }

    /// <summary>Opdaterer en film (erstatter alle redigerbare felter).</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMovie(uint id, UpdateMovieDto dto, CancellationToken cancellationToken)
    {
        var updated = await _movieService.UpdateMovieAsync(id, dto, cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>Sletter en film. Filmens ratings og reviews slettes også (ON DELETE CASCADE).</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMovie(uint id, CancellationToken cancellationToken)
    {
        var deleted = await _movieService.DeleteMovieAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
