namespace RottenPotatoes.Api.DTOs;

/// <summary>Svar-format for paginerede lister.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
