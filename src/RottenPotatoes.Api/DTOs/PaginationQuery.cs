namespace RottenPotatoes.Api.DTOs;

/// <summary>Query-parametre til pagination og sortering (?page=1&amp;pageSize=20&amp;sortBy=title).</summary>
public class PaginationQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    // TODO: Validering (fx Page >= 1, øvre grænse for PageSize, tilladte SortBy-værdier).
    // TODO: Filtrering – tilføj filterfelter her eller i en ressource-specifik query-DTO.
}
