using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
using System.IO;
using System.Web.Hosting;

namespace WebAppRBACDotNet.Models
{
    public class RoleMapping
    {
        // Every ObjectID<-->Application Role mapping has an ObjectID, a Role, and a Mapping ID
        public int RoleMappingID { get; set; }
        public string ObjectId { get; set; }
        public string Role { get; set; }
    }
}