using System;
using System.Collections.Generic;
using System.Web;

//The following libraries were added to this sample.
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

//The following libraries were defined and added to this sample.
using WebAppRBACDotNet.Helpers;
using WebAppRBACDotNet.Models;
using WebAppRBACDotNet.Utils;

namespace WebAppRBACDotNet
{
    public partial class Startup
    {
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        // The Authority is the sign-in URL of the tenant.
        // The GraphResourceId the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
      
        private static readonly string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static readonly string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static readonly string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static readonly string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static readonly string postLogoutRedirectUri =
            ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        public static readonly string Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
        private static string graphResourceId = "https://graph.windows.net";

        /// <summary>
        /// Configures OpenIDConnect Authentication & Adds Custom Application Authorization Logic on User Login.
        /// </summary>
        /// <param name="app">The application represented by a <see cref="IAppBuilder"/> object.</param>
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            //Configure OpenIDConnect, register callbacks for OpenIDConnect Notifications
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        // This is also where we hook into a user login event and add our own custom application authorization logic.
                        AuthorizationCodeReceived = context =>
                        {
                            var credential = new ClientCredential(clientId, appKey);
                            string userObjectId = context.AuthenticationTicket.Identity.FindFirst(
                                "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                            // Configure a new <see cref="NaiveSessionCache" \> for storing the access token
                            // and making it accessible throughout the entire application.
                            var authContext = new AuthenticationContext(Authority, new NaiveSessionCache(userObjectId));
                            
                            // Acquire Access Token
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential,
                                graphResourceId);

                            // Add Appropriate Application Roles To <see cref="AuthenticationTicket" \> object.
                            // The Owin Middleware has not populated the <see cref="ClaimsPrinicpal"> object with 
                            // AuthenticationTicket data at this point, so we must add custom application claims to 
                            // AuthenticationTicket.Identity.
                            AddRolesToAuthTicket(userObjectId, result.AccessToken, context.AuthenticationTicket.Identity);

                            return Task.FromResult(0);
                        }
                    }
                });
        }


        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds all appropriate application roles to the <see cref="AuthenticationTicket"/> object, based
        /// on Built-In AAD roles, Security Group membership, and existing role assingments in our Roles.xml file.
        /// </summary>
        /// <param name="userObjectId">The ObjectID of the signed-in user.</param>
        /// <param name="accessToken">The access token acquired in OpenIDConnect Authentication.</param>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        private void AddRolesToAuthTicket(string userObjectId, string accessToken, ClaimsIdentity claimsIdentity)
        {   
            // A list of all the ObjectIDs of the Security Groups associated with the user
            var listOfGroupObjectIDs = new List<String>();

            // Check if the access token recieved contains an Overage claim.  An overage claim is included
            // if the user is associated with too many Group Claims to fit in the token (>250).
            // If this is the case, we must use the GraphAPI to retrieve group information.
            if (claimsIdentity.FindFirst("_claim_sources") != null)
                listOfGroupObjectIDs = GetGroupsFromGraphAPI(accessToken, userObjectId, claimsIdentity);
            else
            {
                // If no Overage Claim, add each group claim to the ObjectID list for comparison to XML records.
                foreach (Claim groupClaim in claimsIdentity.FindAll("groups"))
                    listOfGroupObjectIDs.Add(groupClaim.Value);
            }

            // In addition, we need to make sure AAD Global Administrators recieve the Application Role "Admin"
            AddGlobalAdminMapping(accessToken, claimsIdentity);

            // For each role the user has been granted, add a role claim to the AuthenticationTicket.Identity object.
            // The application will look at these claims to determine access to different components using 
            // the AuthorizeAttribute class and IsInRole() method.
            foreach (string role in GetRoles(userObjectId, listOfGroupObjectIDs))
            {
                //Store the user's application roles as claims of type Role
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String,
                    "RBAC-Sample-ADALv2-App"));
            }
        }

        /// <summary>
        /// Determine which application roles (Admin, Writer, Approver, Observer) the user has been granted.
        /// </summary>
        /// <param name="objectId"> The signed-in user's ObjectID.</param>
        /// <param name="groupMemberships">The list of <see cref="Group" /> ObjectIDs the signed-in
        /// user belongs to.</param>
        /// <returns>A List of Application Roles that the user has been granted</returns>
        private static List<String> GetRoles(string objectId, List<String> groupMemberships)
        {
            var roles = new List<string>();

            // Make sure the Roles.xml file exists and we have an ObjectID for the user
            if (!File.Exists(RoleMapElem.RoleMapXMLFilePath) || objectId == null)
            {
                return roles;
            }
            
            // For every Application Role in Roles.xml, check if the user's ObjectID or
            // one of their security group's ObjectIDs has been assinged to the role
            foreach (var roleType in XmlHelper.GetRoleMappingsFromXml())
            {
                foreach (RoleMapElem mappingEntry in roleType)
                {
                    if (mappingEntry.ObjectId.Equals(objectId) ||
                        groupMemberships != null && groupMemberships.Contains(mappingEntry.ObjectId))
                    {
                        roles.Add(mappingEntry.Role);
                    }
                }
            }
            return roles;
        }

        /// <summary>
        /// If the access token recieved contains an overage claim, we must query the GraphAPI
        /// to obtain information about the user and the security groups they are a member of.
        /// </summary>
        /// <param name="accessToken">The OpenIDConnect access token, used here for querying the GraphAPI.</param>
        /// <param name="userObjectId">The signed-in user's ObjectID</param>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <returns>A list of ObjectIDs representing the groups that the user is a member of.</returns>
        private List<String> GetGroupsFromGraphAPI(string accessToken, string userObjectId,
            ClaimsIdentity claimsIdentity)
        {
            var pagedResults = new PagedResults<GraphObject>();
            var builtInRolesAndGroups = new List<GraphObject>();
            var listOfGroupObjectIDs = new List<String>();

            try
            {
                // Setup Graph API connection
                Guid ClientRequestId = Guid.NewGuid();
                var graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

                // Query the Graph API to get a User by ObjectID and subsequently 
                // the Groups & Built-In Roles that the user is assinged to.
                var user = graphConnection.Get<User>(userObjectId);
                pagedResults = graphConnection.GetLinkedObjects(user, LinkProperty.MemberOf, null);
                
                // Add All Objects (Both Built-In Directory Roles and Groups) returened by the GraphAPI.
                builtInRolesAndGroups.AddRange(pagedResults.Results);
                while (!pagedResults.IsLastPage)
                {
                    pagedResults = graphConnection.GetLinkedObjects(user, LinkProperty.MemberOf, pagedResults.PageToken);
                    builtInRolesAndGroups.AddRange(pagedResults.Results);
                }
            }
            catch (Exception e)
            {
                //Ignore user not found exception, simply don't add groups
            }

            // For each object returned by the GraphAPI
            foreach (GraphObject roleOrGroup in builtInRolesAndGroups)
            {
                listOfGroupObjectIDs.Add(roleOrGroup.ObjectId);
                claimsIdentity.AddClaim(new Claim("groups", roleOrGroup.ObjectId, ClaimValueTypes.String, "AAD-Tenant-Security-Groups"));
            }

            return listOfGroupObjectIDs;
        }


        /// <summary>
        /// Checks to see if the user is an AAD Global Administrator. If so, assigns the "Global Administrator" group object
        /// the application role of "Admin" by adding its ObjectID to the Roles.xml file.  This is to ensure that
        /// at least one Global Administrator can initially login and assign application roles to other users. 
        /// There are several other ways to accomplish this.  For instance, we could manually add the 
        /// Global Administrator group ObjectID to the XML the first time only, saving extra GraphAPI calls made on login.</summary>
        /// <param name="accessToken">The OpenIDConnect access token, used here to query the GraphAPI.</param>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        private void AddGlobalAdminMapping(string accessToken, ClaimsIdentity claimsIdentity)
        {
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            //Check if any of the group claims are the Global Administrator Group, and add to XML if so.
            foreach (Claim groupClaim in claimsIdentity.FindAll("groups"))
            {
                try
                {
                    var resultRole = graphConnection.Get<Role>(groupClaim.Value);
                    if (resultRole.DisplayName == "Company Administrator")
                    {
                        XmlHelper.AppendRoleMappingToXml("Admin", groupClaim.Value);
                        return;
                    }
                }
                catch (Exception e) 
                {
                    //Ignore object not found exception, only looking for Roles
                }
            }
        }

        ///// <summary>
        ///// TODO: This to replace above implementation once GraphAPI 1.5 is available.
        ///// 
        ///// Assigns the "Global Administrator" group object
        ///// the application role of "Admin" by adding its ObjectID to the Roles.xml file.  This is to ensure that
        ///// at least one Global Administrator can initially login and assign application roles to other users. 
        ///// There are several other ways to accomplish this.</summary>
        ///// <param name="accessToken">The OpenIDConnect access token, used here to query the GraphAPI.</param>
        ///// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        ///// claims-based identity of the currently signed in user and contains thier claims.</param>
        //private void AddGlobalAdminMapping(string accessToken, ClaimsIdentity claimsIdentity)
        //{
        //    // Setup Graph API connection
        //    Guid ClientRequestId = Guid.NewGuid();
        //    var graphSettings = new GraphSettings();
        //    graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
        //    var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

        //    //// With Role Filter Search
        //    //try
        //    //{
        //    //    var filter = new FilterGenerator();
        //    //    filter.QueryFilter =
        //    //        ExpressionHelper.CreateConditionalExpression(
        //    //            typeof (Role), GraphProperty.DisplayName, "Company Administrator", ExpressionType.Equal);
        //    //    PagedResults<Role> pagedRoleResults = graphConnection.List<Role>(null, filter);
        //    //    XmlHelper.AppendRoleMappingToXml("Admin", pagedRoleResults.Results[0].ObjectId);
                
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //}

        //    //// Without Role Filter Search
        //    //try
        //    //{
        //    //    PagedResults<Role> roleList = graphConnection.List<Role>(null, null);
        //    //    foreach (Role role in roleList.Results)
        //    //    {
        //    //        if (role.DisplayName == "Company Administrator")
        //    //            XmlHelper.AppendRoleMappingToXml("Admin", role.ObjectId);
        //    //    }
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //}
        //}
    }
}