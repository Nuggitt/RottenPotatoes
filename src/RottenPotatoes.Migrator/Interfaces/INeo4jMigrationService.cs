namespace RottenPotatoes.Migrator.Interfaces;

/// <summary>Engangsmigrering MySQL -> Neo4j (svarer til MigrateToNeo4j.ts).</summary>
public interface INeo4jMigrationService
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
