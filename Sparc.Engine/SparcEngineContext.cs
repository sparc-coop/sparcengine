﻿using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;
using Sparc.Engine;

internal class SparcEngineContext(DbContextOptions<SparcEngineContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SparcUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);
        
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
    }
}