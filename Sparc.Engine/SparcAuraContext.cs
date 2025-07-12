using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;

namespace Sparc.Aura;

internal class SparcAuraContext(DbContextOptions<SparcAuraContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);
    }
}