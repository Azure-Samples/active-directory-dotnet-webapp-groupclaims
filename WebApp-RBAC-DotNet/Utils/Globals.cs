using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
using System.Configuration;

namespace WebAppRBACDotNet.Utils
{
    public static class Globals
    {
        private const string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string graphResourceId = "https://graph.windows.net";
        private static List<String> roles = new List<String>(new String[5] {"Owner", "Admin", "Observer", "Writer", "Approver"});
        private static List<String> taskStatuses = new List<String>(new String[4] {"NotStarted", "InProgress", "Complete", "Blocked"});

        internal static string ObjectIdClaimType { get { return objectIdClaimType; } }
        public static string ClientId { get { return clientId; } }
        internal static string AppKey { get { return appKey; } }
        public static string GraphResourceId { get { return graphResourceId; } }
        public static List<String> Roles { get { return roles; } }
        public static List<String> Statuses { get { return taskStatuses; } }
        
    }
}