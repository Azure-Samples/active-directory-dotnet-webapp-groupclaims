using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Globalization;
using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using RBACSampleADALv2.Utils;
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using RBACSampleADALv2.Models;
using RBACSampleADALv2.Helpers;

namespace RBACSampleADALv2
{
    public partial class Startup
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        // The Metadata Address is used by the application to retrieve the signing keys used by Azure AD.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        //
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        public static readonly string Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        string graphResourceId = "https://graph.windows.net";

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        //
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        //
                        AuthorizationCodeReceived = (context) =>
                        {
                            var code = context.Code;

                            ClientCredential credential = new ClientCredential(clientId, appKey);
                            string userObjectID =
                                context.AuthenticationTicket.Identity.FindFirst(
                                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                            //string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value; //TODO: Bug on Github Sample?
                            AuthenticationContext authContext = new AuthenticationContext(Authority, new NaiveSessionCache(userObjectID));
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceId);

                            addExistingRolesToClaimPrincipal(credential, userObjectID, authContext, result.AccessToken, context.AuthenticationTicket.Identity);

                            return Task.FromResult(0);
                        }
                    }

                });
        }

        private void addExistingRolesToClaimPrincipal(ClientCredential credential, string userObjectId, AuthenticationContext authContext, string accessToken, ClaimsIdentity claimsIdentity)
        {
            //Call the Graph API for Role and Group Membership //TODO: Change this shit to use Role & Group Claims (this is the old way)
            PagedResults<GraphObject> builtInRolesAndGroups = new PagedResults<GraphObject>();
            try
            {
                // Setup Graph API connection
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

                //Try to get a User by ObjectID
                User user = graphConnection.Get<User>(userObjectId);
                builtInRolesAndGroups = graphConnection.GetLinkedObjects(user, LinkProperty.MemberOf,
                    null);

                //TODO: ^^That's a Paged Results... Use a While Loop
            }
            catch (Exception e) {}

            List<String> listOfGroupObjectIds = new List<String>();
            foreach (var roleOrGroup in builtInRolesAndGroups.Results)
            {
                if (roleOrGroup.ODataTypeName == "Microsoft.WindowsAzure.ActiveDirectory.Role" && roleOrGroup.TokenDictionary["displayName"].ToString() == "Company Administrator")
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "RBAC-Sample-ADALv2-App"));
                }
                else if (roleOrGroup.ODataTypeName == "Microsoft.WindowsAzure.ActiveDirectory.Group")
                {
                    listOfGroupObjectIds.Add(roleOrGroup.ObjectId);
                    claimsIdentity.AddClaim(new Claim("Group", roleOrGroup.ObjectId, ClaimValueTypes.String, "AAD-Tenant-Security-Groups"));

                }
            }

            foreach (string role in getRoles(userObjectId, listOfGroupObjectIds))
            {
                //Store the user's application roles as claims of type Role
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "RBAC-Sample-ADALv2-App"));
            }
        }

        private static List<String> getRoles(string objectId, List<String> groupMemberships)
        {
            List<string> roles = new List<string>();

            if (!File.Exists(RoleMapElem.RoleMapXMLFilePath) || objectId == null)
            {
                return roles;
            }

            foreach (List<RoleMapElem> list in XmlHelper.GetRoleMappingsFromXml())
            {
                foreach (RoleMapElem elem in list)
                {
                    if (elem.ObjectId.Equals(objectId) || groupMemberships != null && groupMemberships.Contains(elem.ObjectId))
                    {
                        roles.Add(elem.Role);
                    }
                }
            }
            return roles;
        }
    }
}