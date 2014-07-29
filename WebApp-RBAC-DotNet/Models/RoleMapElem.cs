//The following libraries were added to this sample.
using System.IO;
using System.Web.Hosting;

namespace WebAppRBACDotNet.Models
{
    public class RoleMapElem
    {
        /// <summary>
        /// The Application Roles for our Task Tracker Application
        /// </summary>
        public static string[] Roles = { "Admin", "Observer", "Writer", "Approver" };

        /// <summary>
        /// The file location of Roles.xml
        /// </summary>
        public static string RoleMapXMLFilePath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data", "Roles.xml");
        
        // Every ObjectID<-->Application Role mapping has an ObjectID, a Role, and a Mapping ID
        public string ObjectId { get; set; }
        public string Role { get; set; }
        public string MappingId { get; set; }

        //Parameterless constructor required for xml serialization
        public RoleMapElem() { }

        public RoleMapElem(string objectId, string role, string mappingId)
        {
            this.ObjectId = objectId;
            this.Role = role;
            this.MappingId = mappingId;
        }

    }
}