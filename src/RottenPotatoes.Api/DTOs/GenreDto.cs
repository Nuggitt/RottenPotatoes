namespace RottenPotatoes.Api.DTOs;

/// <summary>Genre som den vises i API'et. Id kan bruges i filteret GenreId.</summary>
public class GenreDto
{
    public ushort Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
