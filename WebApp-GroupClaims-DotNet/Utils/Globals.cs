using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
using System.Configuration;
using System.Globalization;

namespace WebAppGroupClaimsDotNet.Utils
{
    public static class Globals
    {
        private static string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string tenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string surnameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname";
        private const string givennameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname";
        private static List<String> roles = new List<String>(new String[4] { "Admin", "Observer", "Writer", "Approver" });
        private static List<String> taskStatuses = new List<String>(new String[4] { "NotStarted", "InProgress", "Complete", "Blocked" });

        internal static string ObjectIdClaimType { get { return objectIdClaimType; } }
        internal static string TenantIdClaimType { get { return tenantIdClaimType; } }
        internal static string SurnameClaimType { get { return surnameClaimType; } }
        internal static string GivennameClaimType { get { return givennameClaimType; } }
        public static List<String> Roles { get { return roles; } }
        public static List<String> Statuses { get { return taskStatuses; } }
        
    }
}