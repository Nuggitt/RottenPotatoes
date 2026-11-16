using MongoDB.Bson.Serialization.Conventions;

namespace RottenPotatoes.Migrator.Models.Mongo;

public static class MongoConventions
{
    /// <summary>
    /// camelCase-feltnavne (Title -> title), så dokumenterne matcher designet i
    /// database/mongodb/design/. Skal kaldes, før der skrives til MongoDB.
    /// </summary>
    public static void Register()
    {
        var pack = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("RottenPotatoes camelCase", pack, type => type.Namespace == typeof(MongoConventions).Namespace);
    }
}
