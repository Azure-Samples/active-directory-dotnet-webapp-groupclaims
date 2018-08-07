using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApp_GroupClaims_DotNet.Models
{
    public class UserGroupsAndDirectoryRoles
    {
        public UserGroupsAndDirectoryRoles()
        {
            this.GroupIds = new List<string>();
            this.Groups = new List<Group>();
            this.DirectoryRoles = new List<DirectoryRole>();
        }

        public bool HasOverageClaim { get; set; }

        public List<string> GroupIds{ get; set; }

        public List<Group> Groups { get; set; }

        /// <summary>
        /// Gets or sets the App roles
        /// </summary>
        public List<DirectoryRole> DirectoryRoles { get; set; }
        
    }
}