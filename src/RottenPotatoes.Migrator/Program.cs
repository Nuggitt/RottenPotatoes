using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Neo4j.Driver;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Migrator.Interfaces;
using RottenPotatoes.Migrator.Models.Mongo;
using RottenPotatoes.Migrator.Services;

// Engangsmigrering: MySQL -> MongoDB og Neo4j.
// Konfiguration: appsettings.json + miljøvariabler fra compose.yaml (passwords kommer fra .env).
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
var config = builder.Configuration;

builder.Services.AddRottenPotatoesData(config.GetConnectionString("RottenPotatoes"));

// Klienterne forbinder først, når de bruges.
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(Required(config, "ConnectionStrings:MongoDb")));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(Required(config, "MongoDb:DatabaseName")));
builder.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
    Required(config, "Neo4j:Uri"),
    AuthTokens.Basic(Required(config, "Neo4j:Username"), Required(config, "Neo4j:Password"))));

builder.Services.AddScoped<IMySqlDataReader, MySqlDataReader>();
builder.Services.AddScoped<IMongoMigrationService, MongoMigrationService>();
builder.Services.AddScoped<INeo4jMigrationService, Neo4jMigrationService>();

// camelCase-feltnavne i MongoDB (title, releaseDate ...) som i designet.
MongoConventions.Register();

// Hvilken database der migreres til: "mongo", "neo4j" eller begge (intet argument).
// Bruges fx efter en vellykket Mongo-migrering: make migrator TARGET=neo4j
var target = args.FirstOrDefault()?.ToLowerInvariant();
if (target is not (null or "mongo" or "neo4j"))
{
    Console.Error.WriteLine($"Ukendt mål '{target}'. Brug mongo, neo4j eller intet (begge).");
    return 1;
}

using var host = builder.Build();
using var scope = host.Services.CreateScope();

try
{
    if (target is null or "mongo")
    {
        await scope.ServiceProvider.GetRequiredService<IMongoMigrationService>().MigrateAsync();
        Console.WriteLine("MongoDB: migrering gennemført.");
    }

    if (target is null or "neo4j")
    {
        await scope.ServiceProvider.GetRequiredService<INeo4jMigrationService>().MigrateAsync();
        Console.WriteLine("Neo4j: migrering gennemført.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migreringen fejlede: {ex.Message}");
    return 1;
}

return 0;

static string Required(IConfiguration config, string key)
{
    var value = config[key];
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Konfigurationen '{key}' mangler (miljøvariabel: {key.Replace(":", "__")}).")
        : value;
}
