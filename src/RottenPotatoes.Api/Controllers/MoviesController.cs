using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;

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

    // TODO: Kald _movieService og returnér passende statuskoder (200, 201, 204, 400, 404).
    // TODO: [Authorize]/[Authorize(Roles = "...")] når authentication er på plads.

    [HttpGet]
    public Task<ActionResult<PagedResult<MovieDto>>> GetMovies([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => throw new NotImplementedException();

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

    [HttpPost]
    public Task<ActionResult<MovieDto>> CreateMovie(CreateMovieDto dto, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpPut("{id}")]
    public Task<IActionResult> UpdateMovie(uint id, UpdateMovieDto dto, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpDelete("{id}")]
    public Task<IActionResult> DeleteMovie(uint id, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
