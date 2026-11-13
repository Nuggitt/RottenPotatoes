using System.ComponentModel.DataAnnotations;

namespace RottenPotatoes.Api.DTOs;

/// <summary>Fælles query-parametre til pagination og sortering.</summary>
public class PaginationQuery
{
    /// <summary>Sidenummer. Starter ved 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Antal elementer pr. side (1–100).</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>Feltet der sorteres efter. Tilladte værdier afhænger af ressourcen.</summary>
    public string? SortBy { get; set; }

    /// <summary>true = faldende (Z–A, højeste først).</summary>
    public bool SortDescending { get; set; }
}
