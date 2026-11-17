using Microsoft.Extensions.Configuration;
using Neo4j.Driver;
using RottenPotatoes.Migrator.Interfaces;
using RottenPotatoes.Migrator.Mappings;

namespace RottenPotatoes.Migrator.Services;

/// <summary>
/// Engangsmigrering MySQL -> Neo4j (svarer til MigrateToNeo4j.ts). Design: docs/database-mapping.md.
///
/// Forskelle fra underviserens version:
/// - Rækker sendes i batches med UNWIND $rows (ét kald pr. 5.000 rækker i stedet for ét pr. node/edge).
/// - Databasen slettes/genoprettes aldrig. Findes der allerede noder med vores labels, stopper migreringen.
/// - Neo4j's egne tællere (nodes/edges oprettet) kontrolleres mod antallet af rækker fra MySQL.
/// </summary>
public class Neo4jMigrationService : INeo4jMigrationService
{
    private const int BatchSize = 5_000;

    // Unikke constraints fra det eksisterende design (database/neo4j/design/constraints.cypher).
    // De giver også det index på id, som MATCH (n:Label {id: ...}) bruger, når edges oprettes.
    private static readonly (string Name, string Label)[] Constraints =
    [
        ("actor_id_unique", "Actor"),
        ("award_id_unique", "Award"),
        ("director_id_unique", "Director"),
        ("genre_id_unique", "Genre"),
        ("movie_id_unique", "Movie"),
        ("rating_id_unique", "Rating"),
        ("review_id_unique", "Review"),
        ("series_id_unique", "Series"),
        ("streaming_service_id_unique", "StreamingService"),
        ("user_activity_id_unique", "UserActivity"),
        ("user_id_unique", "User"),
    ];

    private readonly IMySqlDataReader _reader;
    private readonly IDriver _driver;
    private readonly string _databaseName;

    public Neo4jMigrationService(IMySqlDataReader reader, IDriver driver, IConfiguration configuration)
    {
        _reader = reader;
        _driver = driver;
        _databaseName = configuration["Neo4j:Database"] ?? "neo4j";
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Neo4j: migrerer til databasen '{_databaseName}' ...");

        await EnsureNoExistingDataAsync();
        await CreateConstraintsAsync();

        // 1. Noder uden afhængigheder
        await CreateNodesAsync("Genre", "CREATE (:Genre {id: row.id, name: row.name})",
            (await _reader.GetGenresAsync(cancellationToken)).Select(Neo4jMappings.ToRow));
        await CreateNodesAsync("Award", "CREATE (:Award {id: row.id, name: row.name})",
            (await _reader.GetAwardsAsync(cancellationToken)).Select(Neo4jMappings.ToRow));
        await CreateNodesAsync("StreamingService", "CREATE (:StreamingService {id: row.id, name: row.name})",
            (await _reader.GetStreamingServicesAsync(cancellationToken)).Select(Neo4jMappings.ToRow));
        await CreateNodesAsync("Actor",
            "CREATE (:Actor {id: row.id, name: row.name, dateOfBirth: date(row.dateOfBirth), biography: row.biography})",
            (await _reader.GetActorsAsync(cancellationToken)).Select(Neo4jMappings.ToRow));
        await CreateNodesAsync("Director",
            "CREATE (:Director {id: row.id, name: row.name, dateOfBirth: date(row.dateOfBirth), biography: row.biography})",
            (await _reader.GetDirectorsAsync(cancellationToken)).Select(Neo4jMappings.ToRow));
        await CreateNodesAsync("User", "CREATE (:User {id: row.id, username: row.username, role: row.role})",
            (await _reader.GetUsersAsync(cancellationToken)).Select(Neo4jMappings.ToRow));

        // 2. Film og serier + edges fra junction-tabellerne
        await MigrateMoviesAsync(cancellationToken);
        await MigrateSeriesAsync(cancellationToken);

        // 3. Ratings og reviews (500.000 + 200.000) i batches direkte fra MySQL
        await MigrateRatingsAsync(cancellationToken);

        // 4. Brugeraktivitet (aktive + arkiverede)
        await MigrateUserActivitiesAsync(cancellationToken);
    }

    private async Task EnsureNoExistingDataAsync()
    {
        var existing = new List<string>();

        foreach (var (_, label) in Constraints)
        {
            // Label-navne kommer fra listen ovenfor (faste strenge), ikke fra brugerinput.
            var summary = await RunAsync($"MATCH (n:{label}) RETURN count(n) AS count", new());
            if (summary.Count > 0)
            {
                existing.Add(label);
            }
        }

        if (existing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Neo4j indeholder allerede noder med labels: {string.Join(", ", existing)}. " +
                "Migratoren sletter ikke eksisterende data, så intet er ændret. " +
                "Nulstil Neo4j manuelt, hvis du vil migrere igen (se README).");
        }
    }

    private async Task CreateConstraintsAsync()
    {
        foreach (var (name, label) in Constraints)
        {
            await RunAsync($"CREATE CONSTRAINT {name} IF NOT EXISTS FOR (n:{label}) REQUIRE n.id IS UNIQUE", new());
        }
    }

    private async Task MigrateMoviesAsync(CancellationToken cancellationToken)
    {
        var movies = await _reader.GetMoviesWithRelationsAsync(cancellationToken);

        await CreateNodesAsync("Movie",
            """
            CREATE (:Movie {id: row.id, title: row.title, duration: row.duration, releaseDate: date(row.releaseDate),
                            resume: row.resume, language: row.language, ageRating: row.ageRating})
            """,
            movies.Select(Neo4jMappings.ToRow));

        await CreateEdgesAsync("Actor", "ACTED_IN", "Movie",
            movies.SelectMany(m => m.Actors.Select(a => Neo4jMappings.Edge(a.Id, m.Id))));
        await CreateEdgesAsync("Director", "DIRECTED", "Movie",
            movies.SelectMany(m => m.Directors.Select(d => Neo4jMappings.Edge(d.Id, m.Id))));
        await CreateEdgesAsync("Movie", "IN_GENRE", "Genre",
            movies.SelectMany(m => m.Genres.Select(g => Neo4jMappings.Edge(m.Id, g.Id))));
        await CreateEdgesAsync("Movie", "WON", "Award",
            movies.SelectMany(m => m.Awards.Select(a => Neo4jMappings.Edge(m.Id, a.Id))));
        await CreateEdgesAsync("Movie", "AVAILABLE_ON", "StreamingService",
            movies.SelectMany(m => m.StreamingServices.Select(s => Neo4jMappings.Edge(m.Id, s.Id))));
    }

    private async Task MigrateSeriesAsync(CancellationToken cancellationToken)
    {
        var series = await _reader.GetSeriesWithRelationsAsync(cancellationToken);

        await CreateNodesAsync("Series",
            """
            CREATE (:Series {id: row.id, title: row.title, releaseDate: date(row.releaseDate), resume: row.resume,
                             language: row.language, ageRating: row.ageRating,
                             seasons: row.seasons, episodeCount: row.episodeCount})
            """,
            series.Select(Neo4jMappings.ToRow));

        await CreateEdgesAsync("Actor", "ACTED_IN", "Series",
            series.SelectMany(s => s.Actors.Select(a => Neo4jMappings.Edge(a.Id, s.Id))));
        await CreateEdgesAsync("Director", "DIRECTED", "Series",
            series.SelectMany(s => s.Directors.Select(d => Neo4jMappings.Edge(d.Id, s.Id))));
        await CreateEdgesAsync("Series", "IN_GENRE", "Genre",
            series.SelectMany(s => s.Genres.Select(g => Neo4jMappings.Edge(s.Id, g.Id))));
        await CreateEdgesAsync("Series", "WON", "Award",
            series.SelectMany(s => s.Awards.Select(a => Neo4jMappings.Edge(s.Id, a.Id))));
        await CreateEdgesAsync("Series", "AVAILABLE_ON", "StreamingService",
            series.SelectMany(s => s.StreamingServices.Select(st => Neo4jMappings.Edge(s.Id, st.Id))));
    }

    private async Task MigrateRatingsAsync(CancellationToken cancellationToken)
    {
        int ratings = 0, reviews = 0;

        await foreach (var batch in _reader.GetRatingsWithReviewsInBatchesAsync(BatchSize, cancellationToken))
        {
            var ratingRows = batch.Select(Neo4jMappings.ToRow).ToList();
            var reviewRows = batch.Where(r => r.Review is not null).Select(r => Neo4jMappings.ToRow(r.Review!)).ToList();

            // (User)-[:GAVE]->(Rating)
            await RunAndVerifyAsync(
                """
                UNWIND $rows AS row
                MATCH (u:User {id: row.userId})
                CREATE (u)-[:GAVE]->(:Rating {id: row.id, score: row.score, createdAt: datetime(row.createdAt)})
                """,
                ratingRows, expectedNodes: ratingRows.Count, expectedEdges: ratingRows.Count, what: "Rating + GAVE");

            // (Rating)-[:RATES]->(Movie) og (Rating)-[:RATES]->(Series)
            var movieRatings = ratingRows.Where(r => r["movieId"] is not null).ToList();
            var seriesRatings = ratingRows.Where(r => r["seriesId"] is not null).ToList();

            await RunAndVerifyAsync(
                """
                UNWIND $rows AS row
                MATCH (r:Rating {id: row.id})
                MATCH (m:Movie {id: row.movieId})
                CREATE (r)-[:RATES]->(m)
                """,
                movieRatings, expectedNodes: 0, expectedEdges: movieRatings.Count, what: "RATES (Movie)");

            await RunAndVerifyAsync(
                """
                UNWIND $rows AS row
                MATCH (r:Rating {id: row.id})
                MATCH (s:Series {id: row.seriesId})
                CREATE (r)-[:RATES]->(s)
                """,
                seriesRatings, expectedNodes: 0, expectedEdges: seriesRatings.Count, what: "RATES (Series)");

            // (Rating)-[:HAS_REVIEW]->(Review)
            await RunAndVerifyAsync(
                """
                UNWIND $rows AS row
                MATCH (r:Rating {id: row.ratingId})
                CREATE (r)-[:HAS_REVIEW]->(:Review {id: row.id, content: row.content, createdAt: datetime(row.createdAt)})
                """,
                reviewRows, expectedNodes: reviewRows.Count, expectedEdges: reviewRows.Count, what: "Review + HAS_REVIEW");

            ratings += ratingRows.Count;
            reviews += reviewRows.Count;
            Console.WriteLine($"  Rating: {ratings} ...");
        }

        Console.WriteLine($"  Rating: {ratings} noder (+ GAVE og RATES)");
        Console.WriteLine($"  Review: {reviews} noder (+ HAS_REVIEW)");
    }

    private async Task MigrateUserActivitiesAsync(CancellationToken cancellationToken)
    {
        var rows = (await _reader.GetUserActivitiesAsync(cancellationToken)).Select(Neo4jMappings.ToRow)
            .Concat((await _reader.GetArchivedUserActivitiesAsync(cancellationToken)).Select(Neo4jMappings.ToRow))
            .ToList();

        await RunAndVerifyAsync(
            """
            UNWIND $rows AS row
            MATCH (u:User {id: row.userId})
            CREATE (u)-[:PERFORMED]->(:UserActivity {id: row.id, activityType: row.activityType,
                                                     createdAt: datetime(row.createdAt), archivedAt: datetime(row.archivedAt)})
            """,
            rows, expectedNodes: rows.Count, expectedEdges: rows.Count, what: "UserActivity + PERFORMED");

        Console.WriteLine($"  UserActivity: {rows.Count} noder (+ PERFORMED)");
    }

    /// <summary>Opretter noder i batches: UNWIND $rows AS row + createClause.</summary>
    private async Task CreateNodesAsync(string label, string createClause, IEnumerable<Dictionary<string, object?>> rows)
    {
        var total = 0;

        foreach (var batch in rows.Chunk(BatchSize))
        {
            await RunAndVerifyAsync($"UNWIND $rows AS row {createClause}", batch, batch.Length, 0, label);
            total += batch.Length;
        }

        Console.WriteLine($"  {label}: {total} noder");
    }

    /// <summary>(from:FromLabel {id})-[:TYPE]->(to:ToLabel {id}) i batches.</summary>
    private async Task CreateEdgesAsync(string fromLabel, string type, string toLabel, IEnumerable<Dictionary<string, object?>> rows)
    {
        // Labels og relationstype er faste strenge i koden; værdier sendes som parametre.
        var query = $$"""
            UNWIND $rows AS row
            MATCH (a:{{fromLabel}} {id: row.from})
            MATCH (b:{{toLabel}} {id: row.to})
            CREATE (a)-[:{{type}}]->(b)
            """;
        var what = $"({fromLabel})-[:{type}]->({toLabel})";
        var total = 0;

        foreach (var batch in rows.Chunk(BatchSize))
        {
            await RunAndVerifyAsync(query, batch, 0, batch.Length, what);
            total += batch.Length;
        }

        Console.WriteLine($"  {what}: {total} edges");
    }

    /// <summary>
    /// Kører én batch i én transaktion og tjekker Neo4j's tællere. Mangler fx en bruger,
    /// finder MATCH ingenting, og rækken springes stille over – det fanges her.
    /// </summary>
    private async Task RunAndVerifyAsync(string query, IReadOnlyCollection<Dictionary<string, object?>> rows,
        int expectedNodes, int expectedEdges, string what)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var summary = await RunAsync(query, new Dictionary<string, object> { ["rows"] = rows });

        if (summary.Counters.NodesCreated != expectedNodes || summary.Counters.RelationshipsCreated != expectedEdges)
        {
            throw new InvalidOperationException(
                $"{what}: forventede {expectedNodes} noder og {expectedEdges} edges, men Neo4j oprettede " +
                $"{summary.Counters.NodesCreated} noder og {summary.Counters.RelationshipsCreated} edges. " +
                "Migreringen er stoppet halvvejs; nulstil Neo4j før næste forsøg (se README).");
        }
    }

    private async Task<(long Count, ICounters Counters)> RunAsync(string query, Dictionary<string, object> parameters)
    {
        await using var session = _driver.AsyncSession(options => options.WithDatabase(_databaseName));

        return await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var summary = await cursor.ConsumeAsync();
            var count = records.Count == 1 && records[0].Keys.Contains("count") ? records[0]["count"].As<long>() : 0;
            return (count, summary.Counters);
        });
    }
}
