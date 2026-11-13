namespace RottenPotatoes.Migrator.Interfaces;

/// <summary>Engangsmigrering MySQL -> MongoDB (svarer til MigrateToMongo.ts).</summary>
public interface IMongoMigrationService
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
