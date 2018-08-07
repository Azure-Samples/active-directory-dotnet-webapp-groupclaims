using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApp_GroupClaims_DotNet.Models
{
    /// <summary>
    /// Represents a Task users create
    /// </summary>
    public class Task
    {
        public int TaskID { get; set; }

        [Required]
        public string TaskText { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Creator { get; set; }

        [Required]
        public string CreatorName { get; set; }

        [Required]
        public virtual ICollection<AadObject> SharedWith { get; set; }
    }
}