using Microsoft.EntityFrameworkCore;

namespace RottenPotatoes.Data.Context;

// PLADSHOLDER – denne fil overskrives af `docker compose run --rm dev make scaffold`.
// Den genererede version indeholder DbSet'er og OnModelCreating for alle tabeller og views
// i rotten_potatoes. Tilføj ikke egen kode her; brug en separat partial-klasse i stedet.
public partial class RottenPotatoesDbContext : DbContext
{
    public RottenPotatoesDbContext(DbContextOptions<RottenPotatoesDbContext> options)
        : base(options)
    {
    }
}
