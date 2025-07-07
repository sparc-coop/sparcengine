using Microsoft.EntityFrameworkCore;

using Sparc.Engine;

internal class TovikContext(DbContextOptions<TovikContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SparcDomain>().ToContainer("Domains")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);
    }
}