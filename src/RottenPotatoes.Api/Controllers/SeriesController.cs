using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;

namespace RottenPotatoes.Api.Controllers;

[ApiController]
[Route("api/series")]
public class SeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;

    public SeriesController(ISeriesService seriesService)
    {
        _seriesService = seriesService;
    }

    // TODO: [Authorize]/[Authorize(Roles = "admin")] når authentication er på plads.

    /// <summary>Henter serier med pagination, filtrering og sortering.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SeriesDto>>> GetSeries([FromQuery] SeriesQuery query, CancellationToken cancellationToken)
    {
        var result = await _seriesService.GetSeriesAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>Henter én serie ud fra id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesDto>> GetSeriesById(uint id, CancellationToken cancellationToken)
    {
        var series = await _seriesService.GetSeriesByIdAsync(id, cancellationToken);

        if (series is null)
        {
            return NotFound();
        }

        return Ok(series);
    }

    /// <summary>Opretter en serie.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SeriesDto>> CreateSeries(CreateSeriesDto dto, CancellationToken cancellationToken)
    {
        var series = await _seriesService.CreateSeriesAsync(dto, cancellationToken);

        // 201 Created + Location-header, der peger på GET /api/series/{id}.
        return CreatedAtAction(nameof(GetSeriesById), new { id = series.Id }, series);
    }

    /// <summary>Opdaterer en serie (erstatter alle redigerbare felter).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeries(uint id, UpdateSeriesDto dto, CancellationToken cancellationToken)
    {
        var updated = await _seriesService.UpdateSeriesAsync(id, dto, cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>Sletter en serie. Seriens ratings og reviews slettes også (ON DELETE CASCADE).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSeries(uint id, CancellationToken cancellationToken)
    {
        var deleted = await _seriesService.DeleteSeriesAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
