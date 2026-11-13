using Neo4j.Driver;
using RottenPotatoes.Migrator.Interfaces;

namespace RottenPotatoes.Migrator.Services;

public class Neo4jMigrationService : INeo4jMigrationService
{
    private readonly IMySqlDataReader _reader;
    private readonly IDriver _driver;

    public Neo4jMigrationService(IMySqlDataReader reader, IDriver driver)
    {
        _reader = reader;
        _driver = driver;
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // TODO: 1. Læs data med _reader.
        // TODO: 2. Opret nodes og relationships (Models/Neo4j + Mappings) efter designet i
        //          docs/database-mapping.md og database/neo4j/design/.
        // TODO: 3. Brug parameteriserede Cypher-queries (fx MERGE ... $id), aldrig strengsammensætning.
        // Regler: ingen MATCH (n) DETACH DELETE n og ingen genoprettelse af databasen.
        // Community Edition har kun databasen "neo4j" (Neo4j:Database i konfigurationen).
        throw new NotImplementedException("Neo4j-migreringen er ikke implementeret endnu.");
    }
}
