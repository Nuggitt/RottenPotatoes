using MongoDB.Driver;
using RottenPotatoes.Migrator.Interfaces;
using RottenPotatoes.Migrator.Mappings;
using RottenPotatoes.Migrator.Models.Mongo;

namespace RottenPotatoes.Migrator.Services;

/// <summary>
/// Engangsmigrering MySQL -> MongoDB (svarer til MigrateToMongo.ts).
/// Sletter aldrig data: hvis en af collections allerede har dokumenter, stopper migreringen
/// før der skrives noget.
/// </summary>
public class MongoMigrationService : IMongoMigrationService
{
    private const int BatchSize = 10_000;

    private static readonly string[] CollectionNames = ["users", "movies", "series", "ratings", "userActivities"];

    private readonly IMySqlDataReader _reader;
    private readonly IMongoDatabase _database;

    public MongoMigrationService(IMySqlDataReader reader, IMongoDatabase database)
    {
        _reader = reader;
        _database = database;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"MongoDB: migrerer til databasen '{_database.DatabaseNamespace.DatabaseName}' ...");

        await EnsureCollectionsAreEmptyAsync(cancellationToken);

        await MigrateUsersAsync(cancellationToken);
        await MigrateMoviesAsync(cancellationToken);
        await MigrateSeriesAsync(cancellationToken);
        await MigrateRatingsAsync(cancellationToken);
        await MigrateUserActivitiesAsync(cancellationToken);
    }

    private async Task EnsureCollectionsAreEmptyAsync(CancellationToken cancellationToken)
    {
        var notEmpty = new List<string>();

        foreach (var name in CollectionNames)
        {
            var count = await _database.GetCollection<object>(name)
                .CountDocumentsAsync(FilterDefinition<object>.Empty, new CountOptions { Limit = 1 }, cancellationToken);

            if (count > 0)
            {
                notEmpty.Add(name);
            }
        }

        if (notEmpty.Count > 0)
        {
            throw new InvalidOperationException(
                $"MongoDB-collections indeholder allerede data: {string.Join(", ", notEmpty)}. " +
                "Migratoren sletter ikke eksisterende data, så intet er ændret. " +
                "Slet databasen manuelt, hvis du vil migrere igen (se README).");
        }
    }

    private async Task MigrateUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _reader.GetUsersAsync(cancellationToken);
        var documents = users.Select(MongoMappings.ToDocument);

        await InsertInBatchesAsync("users", documents, cancellationToken);
    }

    private async Task MigrateMoviesAsync(CancellationToken cancellationToken)
    {
        var movies = await _reader.GetMoviesWithRelationsAsync(cancellationToken);
        var averages = await _reader.GetMovieAverageRatingsAsync(cancellationToken);

        // Gennemsnittet beregnes ud fra Ratings, fordi Movies.AverageRating er 0.00 i testdataene
        // (triggeren kører kun ved nye ratings). Film uden ratings får 0.
        var documents = movies.Select(m => MongoMappings.ToDocument(m, averages.GetValueOrDefault(m.Id)));

        await InsertInBatchesAsync("movies", documents, cancellationToken);
    }

    private async Task MigrateSeriesAsync(CancellationToken cancellationToken)
    {
        var series = await _reader.GetSeriesWithRelationsAsync(cancellationToken);
        var averages = await _reader.GetSeriesAverageRatingsAsync(cancellationToken);

        // Series har ingen AverageRating-kolonne, så den beregnes ud fra Ratings.
        var documents = series.Select(s => MongoMappings.ToDocument(s, averages.GetValueOrDefault(s.Id)));

        await InsertInBatchesAsync("series", documents, cancellationToken);
    }

    private async Task MigrateRatingsAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<RatingDocument>("ratings");
        var total = 0;

        // Læs og skriv i bidder, så alle 500.000 ratings ikke skal ligge i hukommelsen samtidig.
        await foreach (var batch in _reader.GetRatingsWithReviewsInBatchesAsync(BatchSize, cancellationToken))
        {
            await collection.InsertManyAsync(batch.Select(MongoMappings.ToDocument), cancellationToken: cancellationToken);
            total += batch.Count;
            Console.WriteLine($"  ratings: {total} ...");
        }

        Console.WriteLine($"  ratings: {total} dokumenter");
    }

    private async Task MigrateUserActivitiesAsync(CancellationToken cancellationToken)
    {
        var active = await _reader.GetUserActivitiesAsync(cancellationToken);
        var archived = await _reader.GetArchivedUserActivitiesAsync(cancellationToken);

        var documents = active.Select(MongoMappings.ToDocument)
            .Concat(archived.Select(MongoMappings.ToDocument));

        await InsertInBatchesAsync("userActivities", documents, cancellationToken);
    }

    private async Task InsertInBatchesAsync<T>(string collectionName, IEnumerable<T> documents, CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<T>(collectionName);
        var total = 0;

        foreach (var batch in documents.Chunk(BatchSize))
        {
            await collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
            total += batch.Length;
        }

        Console.WriteLine($"  {collectionName}: {total} dokumenter");
    }
}
