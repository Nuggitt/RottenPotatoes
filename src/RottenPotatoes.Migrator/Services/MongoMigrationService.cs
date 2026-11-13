using MongoDB.Driver;
using RottenPotatoes.Migrator.Interfaces;

namespace RottenPotatoes.Migrator.Services;

public class MongoMigrationService : IMongoMigrationService
{
    private readonly IMySqlDataReader _reader;
    private readonly IMongoDatabase _database;

    public MongoMigrationService(IMySqlDataReader reader, IMongoDatabase database)
    {
        _reader = reader;
        _database = database;
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // TODO: 1. Læs data med _reader.
        // TODO: 2. Map entities til dokumentmodeller (Models/Mongo + Mappings) efter designet i
        //          docs/database-mapping.md og database/mongodb/design/.
        // TODO: 3. Indsæt i collections i _database.
        // Regler: slet/drop ikke eksisterende collections, og stop med en fejl hvis
        // collections allerede indeholder data, i stedet for at overskrive dem.
        throw new NotImplementedException("MongoDB-migreringen er ikke implementeret endnu.");
    }
}
