using Microsoft.EntityFrameworkCore;

using Sparc.Engine;

internal class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
    }
}