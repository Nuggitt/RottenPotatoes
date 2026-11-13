namespace RottenPotatoes.Migrator.Interfaces;

/// <summary>
/// Læser alle data fra MySQL via EF Core (svarer til fetchDataFromMysql.ts i underviserens projekt).
/// </summary>
public interface IMySqlDataReader
{
    // TODO: Tilføj læsemetoder, når entities er genereret med `make scaffold`, fx:
    //   Task<List<Movie>> GetMoviesAsync(CancellationToken cancellationToken = default);
    // Brug Include(...) til de relationer, som MongoDB-/Neo4j-designet skal bruge,
    // og AsNoTracking() da data kun læses.
}
