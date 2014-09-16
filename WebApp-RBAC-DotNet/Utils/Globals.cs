using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
using System.Configuration;
using System.Globalization;

namespace WebAppRBACDotNet.Utils
{
    public static class Globals
    {
        private static readonly string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string tenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static List<String> roles = new List<String>(new String[5] {"Owner", "Admin", "Observer", "Writer", "Approver"});
        private static List<String> taskStatuses = new List<String>(new String[4] {"NotStarted", "InProgress", "Complete", "Blocked"});
        private static string graphResourceId = ConfigurationManager.AppSettings["ida:GraphUrl"];
        private static string graphApiVersion = ConfigurationManager.AppSettings["ida:GraphApiVersion"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        public static readonly string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        
        internal static string ObjectIdClaimType { get { return objectIdClaimType; } }
        internal static string TenantIdClaimType { get { return tenantIdClaimType; } }
        public static string ClientId { get { return clientId; } }
        internal static string AppKey { get { return appKey; } }
        public static List<String> Roles { get { return roles; } }
        public static List<String> Statuses { get { return taskStatuses; } }
        internal static string GraphResourceId { get { return graphResourceId; } }
        internal static string GraphApiVersion { get { return graphApiVersion; } }
        internal static string Tenant { get { return tenant; } }
        internal static string Authority { get { return authority; } }
        internal static string AadInstance { get { return aadInstance; } }
        
    }
}