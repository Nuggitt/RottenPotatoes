using RottenPotatoes.Data.Context;
using RottenPotatoes.Migrator.Interfaces;

namespace RottenPotatoes.Migrator.Services;

public class MySqlDataReader : IMySqlDataReader
{
    private readonly RottenPotatoesDbContext _context;

    public MySqlDataReader(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    // TODO: Implementér læsemetoderne fra IMySqlDataReader med _context (efter `make scaffold`).
}
