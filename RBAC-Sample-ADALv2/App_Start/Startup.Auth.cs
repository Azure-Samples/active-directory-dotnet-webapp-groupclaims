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
using System.Linq.Expressions;

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
                            string userObjectID = context.AuthenticationTicket.Identity.FindFirst(
                                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                            //string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value; //TODO: Bug on Github Sample?
                            AuthenticationContext authContext = new AuthenticationContext(Authority, new NaiveSessionCache(userObjectID));
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceId);

                            addRolesToAuthTicket(credential, userObjectID, authContext, result.AccessToken, context.AuthenticationTicket.Identity);

                            return Task.FromResult(0);
                        },

                        RedirectToIdentityProvider = (context) =>
                        {
                            context.ProtocolMessage.SetParameter("slice", "testslice");
                            return Task.FromResult(0);
                        }
                    }

                });
        }

        private void addRolesToAuthTicket(ClientCredential credential, string userObjectId, AuthenticationContext authContext, string accessToken, ClaimsIdentity claimsIdentity)
        {
            List<String> listOfGroupObjectIDs = new List<String>();

            //Check if an Overage Claim came through
            if (claimsIdentity.FindFirst("_claim_sources") != null)
                listOfGroupObjectIDs = getGroupsFromGraphAPI(accessToken, userObjectId, claimsIdentity);
            else
            {
                //If not, Add Group Claims to List To Be Compared to the XML
                foreach (Claim groupClaim in claimsIdentity.FindAll("groups"))
                    listOfGroupObjectIDs.Add(groupClaim.Value);

                //Also, we need to add the Global Administrator Group to the XML as Admins
                //Alternatively, you could manually add the Global Administrator Group to the XML the first time only, saving GraphAPI calls
                checkForGlobalAdmin(accessToken, claimsIdentity);
            }
            
            //Add Application Roles to AuthTicket
            foreach (string role in getRoles(userObjectId, listOfGroupObjectIDs))
            {
                //Store the user's application roles as claims of type Role
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String,
                    "RBAC-Sample-ADALv2-App"));
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


        private List<String> getGroupsFromGraphAPI(string accessToken, string userObjectId, ClaimsIdentity claimsIdentity)
        {
            //Call the Graph API for Role and Group Membership
            PagedResults<GraphObject> pagedResults = new PagedResults<GraphObject>();
            List<GraphObject> builtInRolesAndGroups = new List<GraphObject>();
            List<String> listOfGroupObjectIDs = new List<String>();

            try
            {
                // Setup Graph API connection
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

                //Try to get a User by ObjectID
                User user = graphConnection.Get<User>(userObjectId);
                pagedResults = graphConnection.GetLinkedObjects(user, LinkProperty.MemberOf, null); //TODO: Feature Suggestion. Weird to Get Role & Group Objects as Group Claims. Yes, I know /GetMemberObjects is there. 2 different ObjectIds for "Company Administrator" Role object
                builtInRolesAndGroups.AddRange(pagedResults.Results);
                while (!pagedResults.IsLastPage)
                {
                    pagedResults = graphConnection.GetLinkedObjects(user, LinkProperty.MemberOf, pagedResults.PageToken);
                    builtInRolesAndGroups.AddRange(pagedResults.Results);
                }
            }
            catch (Exception e) { }

            foreach (var roleOrGroup in builtInRolesAndGroups)
            {
                if (roleOrGroup.ODataTypeName == "Microsoft.WindowsAzure.ActiveDirectory.Role" && roleOrGroup.TokenDictionary["displayName"].ToString() == "Company Administrator")
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "RBAC-Sample-ADALv2-App"));
                }
                else if (roleOrGroup.ODataTypeName == "Microsoft.WindowsAzure.ActiveDirectory.Group")
                {
                    listOfGroupObjectIDs.Add(roleOrGroup.ObjectId);
                    claimsIdentity.AddClaim(new Claim("groups", roleOrGroup.ObjectId, ClaimValueTypes.String, "AAD-Tenant-Security-Groups"));

                }
            }

            return listOfGroupObjectIDs;
        }

        private void checkForGlobalAdmin(string accessToken, ClaimsIdentity claimsIdentity)
        {
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            //Check if any of the group claims are actually the "Company Administrator" built in role claim, and add to XML.
            foreach (Claim groupClaim in claimsIdentity.FindAll("groups"))
            {
                try
                {
                    Role resultRole = graphConnection.Get<Role>(groupClaim.Value);
                    if (resultRole.DisplayName == "Company Administrator")
                        XmlHelper.AppendRoleMappingToXml("Admin", groupClaim.Value);
                }
                catch (Exception e) { }
            }
        }
    }
}