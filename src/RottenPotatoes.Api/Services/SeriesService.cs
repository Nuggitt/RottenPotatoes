using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

// Samme opbygning som MovieService. Forskelle: Seasons/Episodes i stedet for Duration,
// og AverageRating beregnes ud fra Ratings, fordi Series ikke har en AverageRating-kolonne.
public class SeriesService : ISeriesService
{
    private readonly RottenPotatoesDbContext _context;

    public SeriesService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    // Seriens gennemsnitlige score. EF laver det til en AVG-subquery i SQL (0 hvis ingen ratings).
    private static readonly Expression<Func<Series, decimal>> AverageRating =
        s => Math.Round(s.Ratings.Average(r => (decimal?)r.Score) ?? 0, 2);

    // Series -> SeriesDto. Bruges i Select, så SQL kun henter det, DTO'en skal bruge.
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
        // Samme beregning som AverageRating ovenfor (EF kan ikke genbruge en Expression inde i en anden).
        AverageRating = Math.Round(s.Ratings.Average(r => (decimal?)r.Score) ?? 0, 2),
        Genres = s.Genres
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name })
            .ToList(),
    };

    public async Task<PagedResult<SeriesDto>> GetSeriesAsync(SeriesQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Series> series = _context.Series.AsNoTracking();

        // 1. Filtrering – hvert filter bruges kun, hvis det er udfyldt.
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

        // 2. Totalantal efter filtrering (før pagination).
        var totalCount = await series.CountAsync(cancellationToken);

        // 3. Sortering – kun faste felter, så brugerens tekst aldrig kommer ind i SQL.
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

        // 4. Pagination. ThenBy(Id) giver en stabil rækkefølge, når flere serier har samme værdi.
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
        await _context.SaveChangesAsync(cancellationToken); // INSERT; MySQL sætter Id, CreatedAt og UpdatedAt

        // Hent serien igen, så svaret indeholder de værdier, databasen har sat.
        return (await GetSeriesByIdAsync(series.Id, cancellationToken))!;
    }

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

    public async Task<bool> DeleteSeriesAsync(uint id, CancellationToken cancellationToken = default)
    {
        // Ét DELETE direkte i databasen. ON DELETE CASCADE fjerner også seriens
        // ratings, reviews og koblinger til genrer, skuespillere osv.
        var deletedRows = await _context.Series
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows > 0;
    }
}
