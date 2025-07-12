using Microsoft.EntityFrameworkCore;

using Sparc.Engine;

internal class TovikContext(DbContextOptions<TovikContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TovikDomain>().ToContainer("Domains")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);

        model.Entity<TextContent>().ToContainer("TextContent")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);
    }
}