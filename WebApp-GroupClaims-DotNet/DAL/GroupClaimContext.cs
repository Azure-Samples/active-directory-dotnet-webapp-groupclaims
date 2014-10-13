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

        public DbSet<RoleMapping> RoleMappings { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TokenCacheEntry> TokenCacheEntries { get; set; }
    }
}