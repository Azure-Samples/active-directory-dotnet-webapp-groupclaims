using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RBACSampleADALv2.Utils
{
    internal static class Globals
    {
        private const string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];

        internal static string ObjectIdClaimType { get { return objectIdClaimType; } }
        internal static string ClientId { get { return clientId; } }
        internal static string AppKey { get { return appKey; } }
    }
}