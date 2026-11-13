using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Neo4j.Driver;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Migrator.Interfaces;
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

using var host = builder.Build();
using var scope = host.Services.CreateScope();

try
{
    await scope.ServiceProvider.GetRequiredService<IMongoMigrationService>().MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<INeo4jMigrationService>().MigrateAsync();
}
catch (NotImplementedException ex)
{
    Console.Error.WriteLine($"Migratoren er ikke færdig: {ex.Message}");
    Console.Error.WriteLine("Migreringen er IKKE gennemført.");
    return 2;
}

// Nås kun, hvis begge migreringer er gennemført uden fejl. Andre fejl afslutter programmet med en exception.
Console.WriteLine("Migrering til MongoDB og Neo4j gennemført.");
return 0;

static string Required(IConfiguration config, string key)
{
    var value = config[key];
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Konfigurationen '{key}' mangler (miljøvariabel: {key.Replace(":", "__")}).")
        : value;
}
