using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RottenPotatoes.Data.Context;

public static class DataServiceCollectionExtensions
{
    // Fast serverversion i stedet for ServerVersion.AutoDetect, så Api og Migrator kan starte,
    // selvom MySQL ikke kører endnu. Matcher mysql:8.4 i compose.yaml.
    private static readonly MySqlServerVersion ServerVersion = new(new Version(8, 4));

    /// <summary>
    /// Registrerer <see cref="RottenPotatoesDbContext"/> med Pomelo MySQL-provideren.
    /// Connection string læses fra konfigurationen (ConnectionStrings:RottenPotatoes),
    /// som i Docker sættes via miljøvariablen ConnectionStrings__RottenPotatoes.
    /// </summary>
    public static IServiceCollection AddRottenPotatoesData(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:RottenPotatoes' mangler. " +
                "Sæt miljøvariablen ConnectionStrings__RottenPotatoes (se compose.yaml og .env).");
        }

        services.AddDbContext<RottenPotatoesDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion));

        return services;
    }
}
