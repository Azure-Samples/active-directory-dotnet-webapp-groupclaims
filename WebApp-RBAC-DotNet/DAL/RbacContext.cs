using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebAppRBACDotNet.Models;

namespace WebAppRBACDotNet.DAL
{
    public class RbacContext : DbContext
    {
        public RbacContext() : base("RbacContext") { }

        public DbSet<RoleMapping> RoleMappings { get; set; }
        public DbSet<Task> Tasks { get; set; }
    }
}