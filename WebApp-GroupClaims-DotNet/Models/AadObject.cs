using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApp_GroupClaims_DotNet.Models
{
    public class AadObject
    {
        public string AadObjectID { get; set; }

        [Required]
        public virtual ICollection<Task> Tasks { get; set; }

        [Required]
        public string DisplayName { get; set; }
    }
}