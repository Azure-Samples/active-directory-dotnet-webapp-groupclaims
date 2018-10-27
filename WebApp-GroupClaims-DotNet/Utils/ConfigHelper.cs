/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using System.Configuration;
using WebApp_GroupClaims_DotNet.Models;

namespace WebApp_GroupClaims_DotNet.Utils
{
    /// <summary>
    /// Wraps the configuration
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        /// </summary>
        public static string AADInstance { get; } = Util.EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);

        /// <summary>
        /// The AppKey is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        /// </summary>
        public static string AppKey { get; } = ConfigurationManager.AppSettings["ida:ClientSecret"];

        /// <summary>
        /// The Client ID is used by the application to uniquely identify itself to Azure AD.
        /// </summary>
        public static string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        /// </summary>
        public static string PostLogoutRedirectUri { get; } = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        /// <summary>
        /// The TenantId is the DirectoryId of the Azure AD tenant being used in the sample
        /// </summary>
        public static string TenantId { get; } = ConfigurationManager.AppSettings["ida:TenantId"];

        /// <summary>
        /// The Authority is the sign-in URL of the tenant.
        /// </summary>
        public static string Authority = AADInstance + TenantId;

        /// <summary>
        /// The GraphResourceId the resource ID of the Microsoft Graph API.  We'll need this to request a token to call the Microsoft Graph API.
        /// </summary>
        public const string GraphResourceId = "https://graph.microsoft.com";

        public static string MSGraphBaseUrl = $"{GraphResourceId}/v1.0";

        /// <summary>
        /// The AADGraphResourceId the resource ID of the AAD Graph API.  We'll need this to request a token to call the Azure AD Graph API.
        /// </summary>
        public static string AADGraphResourceId = "https://graph.windows.net";
    }
}