using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.Models;

namespace WebAppGroupClaimsDotNet.DAL
{
    public class GroupClaimContext : DbContext
    {
        public GroupClaimContext() : base("GroupClaimContext") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Task>().HasMany<AadObject>(t => t.SharedWith).WithMany(a => a.Tasks).Map(m =>
            {
                m.MapLeftKey("TaskID");
                m.MapRightKey("AadObjectID");
                m.ToTable("Shares");
            });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Task> Tasks { get; set; }
        public DbSet<AadObject> AadObjects { get; set; }
        public DbSet<TokenCacheEntry> TokenCacheEntries { get; set; }
    }
}