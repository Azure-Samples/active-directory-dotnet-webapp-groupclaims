using System.Configuration;
using WebApp_GroupClaims_DotNet.Models;

namespace WebApp_GroupClaims_DotNet.Utils
{
    /// <summary>
    /// Wraps the configuration
    /// </summary>
    public static class AppConfig
    {
        public static string AADInstance { get; } = Util.EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        public static string AppKey { get; } = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public static string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];
        public static string PostLogoutRedirectUri { get; } = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        public static string TenantId { get; } = ConfigurationManager.AppSettings["ida:TenantId"];

        public static string Authority = AADInstance + TenantId;

        public const string GraphResourceId = "https://graph.microsoft.com";

        public static string MSGraphBaseUrl = $"{GraphResourceId}/v1.0";

        public static string AADGraphResourceId = "https://graph.windows.net";
    }
}