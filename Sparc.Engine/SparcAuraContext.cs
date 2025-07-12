using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sparc.Aura.Users;

namespace Sparc.Aura;

internal class SparcAuraContext(DbContextOptions<SparcAuraContext> options) : IdentityDbContext<SparcAuraUser>(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        // model.Entity<SparcUser>().ToTable("Users");
    }
}