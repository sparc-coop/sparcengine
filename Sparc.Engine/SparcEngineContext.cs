using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;
using Sparc.Core.Billing;
using Sparc.Core.Chat;
using Sparc.Engine;

internal class SparcEngineContext(DbContextOptions<SparcEngineContext> options) : DbContext(options)
{

    public DbSet<RoomMembership> Memberships { get; set; }
    protected override void OnModelCreating(ModelBuilder model)
    {

        
        model.Entity<PouchDatum>().ToContainer("Data")
            .HasPartitionKey(x => new { x.Db, x.PouchId })
            .HasKey(x => x.Id);

        model.Entity<ReplicationLog>().ToContainer("ReplicationLogs")
            .HasPartitionKey(x => new { x.Db, x.PouchId })
            .HasKey(x => x.Id);

        model.Entity<TextContent>().ToContainer("TextContent")
            .HasPartitionKey(x => new { x.Domain, x.LanguageId })
            .HasKey(x => x.Id);

        model.Entity<SparcDomain>().ToContainer("Domains")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);

        model.Entity<SparcOrder>().ToContainer("Orders")
            .HasPartitionKey(x => x.UserId)
            .HasKey(x => x.Id);

        model.Entity<UserCharge>().ToContainer("UserCharges")
            .HasPartitionKey(x => x.UserId)
            .HasKey(x => x.Id);

        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);

        model.Entity<MatrixRoom>().ToContainer("Rooms")
            .HasPartitionKey(x => x.RoomId)
            .HasKey(x => x.Id);

        model.Entity<RoomMembership>()
          .ToContainer("Memberships")
          .HasPartitionKey(rm => rm.RoomId)
          .HasKey(x => x.Id);

        model.Entity<RoomMembership>()
          .HasOne(rm => rm.Room)
          .WithMany(r => r.Memberships)
          .HasForeignKey(rm => rm.RoomId);

        model.Entity<MatrixEvent>()
          .ToContainer("Events")
          .HasPartitionKey(e => e.RoomId)
          .HasKey(x => x.Id);
    }
}