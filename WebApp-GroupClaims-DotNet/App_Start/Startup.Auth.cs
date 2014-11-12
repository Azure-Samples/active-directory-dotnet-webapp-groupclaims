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
using System.Linq.Expressions;
using System.Linq;
using Owin;
using System.Net.Http;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Text;

//The following libraries were defined and added to this sample.
using WebAppGroupClaimsDotNet.Utils;
using WebAppGroupClaimsDotNet.Models;
using WebAppGroupClaimsDotNet.DAL;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Web.Helpers;

namespace WebAppGroupClaimsDotNet
{
    public partial class Startup
    {
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
                    ClientId = ConfigHelper.ClientId,
                    Authority = ConfigHelper.Authority,
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        SecurityTokenValidated = async context =>
                        {
                            try 
                            {
                                ClaimsIdentity claimsId = context.AuthenticationTicket.Identity;

                                // Check for the existence of a group overage claim (>250 groups)
                                if (claimsId.FindFirst("_claim_names") == null || (Json.Decode(claimsId.FindFirst("_claim_names").Value)).groups == null)
                                {
                                    List<string> groups = new List<string>();
                                    foreach (Claim groupClaim in claimsId.FindAll("groups"))
                                        groups.Add(groupClaim.Value);

                                    // Assign application roles based on group membership
                                    AssignRoles(claimsId.FindFirst(Globals.ObjectIdClaimType).Value, groups, claimsId);
                                }
                            }
                            catch (Exception e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=" + e.Message + "&signIn=true");
                            }

                            return;
                        },

                        AuthorizationCodeReceived = async context =>
                        {
                            try
                            {
                                ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                                string userObjectId = context.AuthenticationTicket.Identity.FindFirst(Globals.ObjectIdClaimType).Value;
                                AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
                                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                    context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, ConfigHelper.GraphResourceId);

                                // Check for the existence of a group overage claim (>250 groups).
                                if (claimsId.FindFirst("_claim_names") != null && (Json.Decode(claimsId.FindFirst("_claim_names").Value)).groups != null)
                                {
                                    List<string> groups = new List<string>();
                                    // Call the GraphAPI to get the user's groups, instead of using Group Claims
                                    groups = await GetGroupsFromGraphAPI(claimsId);
                                    // Assign application roles based on group membership
                                    AssignRoles(userObjectId, groups, claimsId);
                                }

                                // Assign Admin priviliges to the AAD Application Owners, to bootstrap the application on first run.
                                await AddOwnerAdminClaim(userObjectId, context.AuthenticationTicket.Identity);
                            }
                            catch (Exception e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=" + e.Message + "&signIn=true");
                            }

                            return;
                        }
                    }
                });
        }


        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////
        #region HelperFunctions
        /// <summary>
        /// Assign the application roles (Admin, Writer, Approver, Observer) that the user has been granted,
        /// based on our user and group mappings stored in the db.
        /// </summary>
        /// <param name="objectId"> The signed-in user's ObjectID.</param>
        /// <param name="groupMemberships">The list of ObjectIDs of Groups the signed-in user belongs to.</param>
        private void AssignRoles(string userObjectId, List<String> groupMemberships, ClaimsIdentity claimsId)
        {
            GroupClaimContext db = new GroupClaimContext();
            List<RoleMapping> mappings = db.RoleMappings.ToList();
            foreach (RoleMapping mapping in mappings)
            {
                if (mapping.ObjectId.Equals(userObjectId) || (groupMemberships != null && groupMemberships.Contains(mapping.ObjectId)))
                    claimsId.AddClaim(new Claim(ClaimTypes.Role, mapping.Role, ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
            }
        }

        /// <summary>
        /// We must query the GraphAPI to obtain information about the user and the security groups they are a member of.
        /// Here we use the GraphAPI Client Library to do so.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <returns>A list of ObjectIDs representing the groups that the user is a member of.</returns>
        private async Task<List<string>> GetGroupsFromGraphAPI(ClaimsIdentity claimsIdentity)
        {
            List<string> groups = new List<string>();

            // Acquire the Access Token, using the OnBehalfOf flow to exchange the id_token for an access token
            ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority,
                new TokenDbCache(claimsIdentity.FindFirst(Globals.ObjectIdClaimType).Value));
            AuthenticationResult result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential, 
                new UserIdentifier(claimsIdentity.FindFirst(Globals.ObjectIdClaimType).Value, UserIdentifierType.UniqueId));

            // Get the GraphAPI Group Endpoint for the specific user from the _claim_sources claim in token
            string groupsClaimSourceIndex = (Json.Decode(claimsIdentity.FindFirst("_claim_names").Value)).groups;
            var groupClaimsSource = (Json.Decode(claimsIdentity.FindFirst("_claim_sources").Value))[groupsClaimSourceIndex];
            string requestUrl = groupClaimsSource.endpoint + "?api-version=" + ConfigHelper.GraphApiVersion;

            // Prepare and Make the POST request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            StringContent content = new StringContent("{\"securityEnabledOnly\": \"false\"}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
            HttpResponseMessage response = await client.SendAsync(request);

            // Endpoint returns JSON with an array of Group ObjectIDs
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var groupsResult = (Json.Decode(responseContent)).value;

                // For each Group, add its Object ID to the ClaimsIdentity as a Group Claim
                foreach (string groupObjectID in groupsResult) 
                {
                    claimsIdentity.AddClaim(new Claim("groups", groupObjectID, ClaimValueTypes.String, "AAD-Tenant-Groups"));
                    groups.Add(groupObjectID);
                }
            }
            else
            {
                throw new WebException();
            }

            return groups;
        }


        /// <summary>
        /// Checks to see if the signed-in user is an application owner. If so, assigns the user the application 
        /// role "Admin."  This is to ensure that at least one user can login and assign application roles to 
        /// other users the first time we run the app.
        /// </summary>
        /// <param name="claimsId">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <param name="userObjectId"> The ObjectId of the signed in user.</param>
        private async System.Threading.Tasks.Task AddOwnerAdminClaim(string userObjectId, ClaimsIdentity claimsId)
        {
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(new Uri(ConfigHelper.GraphServiceRoot), async () => { return GraphHelper.AcquireToken(userObjectId); });
            IPagedCollection<IApplication> tenantApps = await graphClient.Applications.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            IApplicationFetcher appFetcher = (IApplicationFetcher)tenantApps.CurrentPage[0];
            IPagedCollection<IDirectoryObject> appOwners = await appFetcher.Owners.ExecuteAsync();
            do {
                foreach (IDirectoryObject owner in appOwners.CurrentPage.ToList())
                {
                    if (owner.ObjectId == userObjectId)
                        claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebApp_RoleClaims_DotNet"));
                }
                appOwners = await appOwners.GetNextPageAsync();
            }
            while (appOwners != null);
        }
        #endregion
    }
}