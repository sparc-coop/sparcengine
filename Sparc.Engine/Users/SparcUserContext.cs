using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sparc.Aura.Users;

namespace Sparc.Aura;

internal class SparcUserContext(DbContextOptions<SparcUserContext> options) : IdentityDbContext<SparcUser>(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        // model.Entity<SparcUser>().ToTable("Users");
    }
}