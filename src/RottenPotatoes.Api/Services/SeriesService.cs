using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

/// <summary>
/// Samme opbygning som MovieService. Forskelle: Seasons/Episodes i stedet for Duration,
/// og AverageRating beregnes ud fra Ratings, fordi Series ikke har en AverageRating-kolonne.
/// </summary>
public class SeriesService : ISeriesService
{
    private readonly RottenPotatoesDbContext _context;

    public SeriesService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    /// <summary>Seriens gennemsnitlige score. EF laver det til en AVG-subquery i SQL (0 hvis ingen ratings).</summary>
    private static readonly Expression<Func<Series, decimal>> AverageRating =
        s => Math.Round(s.Ratings.Average(r => (decimal?)r.Score) ?? 0, 2);

    /// <summary>
    /// Series -> SeriesDto. Bruges i Select, så SQL kun henter det, DTO'en skal bruge.
    /// AverageRating er samme beregning som feltet AverageRating ovenfor, fordi EF ikke kan
    /// genbruge en Expression inde i en anden.
    /// </summary>
    private static readonly Expression<Func<Series, SeriesDto>> ToDto = s => new SeriesDto
    {
        Id = s.Id,
        Title = s.Title,
        ReleaseDate = s.ReleaseDate,
        Resume = s.Resume,
        Language = s.Language,
        AgeRating = s.AgeRating,
        Seasons = s.Seasons,
        Episodes = s.Episodes,
        AverageRating = Math.Round(s.Ratings.Average(r => (decimal?)r.Score) ?? 0, 2),
        Genres = s.Genres
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name })
            .ToList(),
    };

    /// <summary>
    /// Filtrerer, tæller (før pagination), sorterer og henter én side. Hvert filter bruges kun,
    /// hvis det er udfyldt. Der sorteres kun på faste felter, så brugerens tekst aldrig kommer ind
    /// i SQL, og ukendt SortBy sorterer efter id. ThenBy(Id) giver en stabil rækkefølge, når flere
    /// serier har samme værdi.
    /// </summary>
    public async Task<PagedResult<SeriesDto>> GetSeriesAsync(SeriesQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Series> series = _context.Series.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            series = series.Where(s => s.Title.Contains(query.Title));
        }

        if (query.GenreId is not null)
        {
            series = series.Where(s => s.Genres.Any(g => g.Id == query.GenreId));
        }

        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            series = series.Where(s => s.Language == query.Language);
        }

        var totalCount = await series.CountAsync(cancellationToken);

        IOrderedQueryable<Series> Sort<TKey>(Expression<Func<Series, TKey>> key) =>
            query.SortDescending ? series.OrderByDescending(key) : series.OrderBy(key);

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "title" => Sort(s => s.Title),
            "releasedate" => Sort(s => s.ReleaseDate),
            "seasons" => Sort(s => s.Seasons),
            "averagerating" => Sort(AverageRating),
            _ => Sort(s => s.Id),
        };

        var items = await sorted
            .ThenBy(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<SeriesDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<SeriesDto?> GetSeriesByIdAsync(uint id, CancellationToken cancellationToken = default)
    {
        return await _context.Series
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// INSERT'er serien med de felter, brugeren må sætte, og henter den igen, så svaret har
    /// de værdier, MySQL selv sætter (Id, CreatedAt og UpdatedAt).
    /// </summary>
    public async Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto, CancellationToken cancellationToken = default)
    {
        var series = new Series
        {
            Title = dto.Title,
            ReleaseDate = dto.ReleaseDate,
            Resume = dto.Resume,
            Language = dto.Language,
            AgeRating = dto.AgeRating,
            Seasons = dto.Seasons,
            Episodes = dto.Episodes,
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetSeriesByIdAsync(series.Id, cancellationToken))!;
    }

    /// <summary>EF holder øje med ændringerne og laver et UPDATE med kun de felter, der er ændret.</summary>
    public async Task<bool> UpdateSeriesAsync(uint id, UpdateSeriesDto dto, CancellationToken cancellationToken = default)
    {
        var series = await _context.Series.FindAsync([id], cancellationToken);

        if (series is null)
        {
            return false;
        }

        series.Title = dto.Title;
        series.ReleaseDate = dto.ReleaseDate;
        series.Resume = dto.Resume;
        series.Language = dto.Language;
        series.AgeRating = dto.AgeRating;
        series.Seasons = dto.Seasons;
        series.Episodes = dto.Episodes;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Ét DELETE direkte i databasen. ON DELETE CASCADE fjerner også seriens
    /// ratings, reviews og koblinger til genrer, skuespillere osv.
    /// </summary>
    public async Task<bool> DeleteSeriesAsync(uint id, CancellationToken cancellationToken = default)
    {
        var deletedRows = await _context.Series
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows > 0;
    }
}
