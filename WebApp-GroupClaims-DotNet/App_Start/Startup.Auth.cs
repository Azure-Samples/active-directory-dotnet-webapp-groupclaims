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
using WebAppGroupClaimsDotNet.Models;
using WebAppGroupClaimsDotNet.Utils;
using WebAppGroupClaimsDotNet.DAL;

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
                        SecurityTokenValidated = context =>
                        {
                            ClaimsIdentity claimsId = context.AuthenticationTicket.Identity;
                            // If we have not received a group claim overage, we can assign the user appropriate roles here.
                            if (claimsId.FindFirst("_claim_sources") == null)
                            {
                                List<String> groupMemberships = new List<String>();
                                foreach (Claim groupClaim in claimsId.FindAll("groups"))
                                    groupMemberships.Add(groupClaim.Value);
                                AssignRoles(claimsId.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value, groupMemberships, claimsId);
                            }

                            return System.Threading.Tasks.Task.FromResult(0);
                        },

                        AuthorizationCodeReceived = async context =>
                        {
                            try
                            {
                                ClaimsIdentity claimsId = context.AuthenticationTicket.Identity;
                                ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                                string userObjectId = claimsId.FindFirst(Globals.ObjectIdClaimType).Value;
                                AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
                                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                    context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, ConfigHelper.GraphResourceId);

                                // If we received a group claims overage (too many groups to fit in the token), 
                                // we need to query for the user's groups using our token for the GraphAPI before assigning roles.
                                if (claimsId.FindFirst("_claim_sources") != null)
                                {
                                    List<String> groupMemberships = await GetGroupsFromGraphAPI(claimsId, userObjectId);
                                    AssignRoles(userObjectId, groupMemberships, claimsId);
                                }

                                // Assign Admin priviliges to the AAD Application Owners, to bootstrap the application on first run.
                                AddOwnerAdminClaim(userObjectId, claimsId);
                            }
                            catch (AdalException e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                            }
                            catch (GraphException e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                            }
                            catch (Exception e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
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
        private async Task<List<String>> GetGroupsFromGraphAPI(ClaimsIdentity claimsIdentity, string userObjectId)
        {
            // Acquire the Access Token
            ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
            AuthenticationResult result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
                new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

            // Get the GraphAPI Group Endpoint for the specific user from the _claim_sources claim in token
            string namesJSON = claimsIdentity.FindFirst("_claim_sources").Value;
            ClaimSource source = JsonConvert.DeserializeObject<ClaimSource>(namesJSON);
            string requestUrl = String.Format(CultureInfo.InvariantCulture, HttpUtility.HtmlEncode(source.src1.endpoint
                + "?api-version=" + ConfigHelper.GraphApiVersion));

            // Prepare and Make the POST request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            StringContent content = new StringContent("{\"securityEnabledOnly\": \"true\"}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
            HttpResponseMessage response = await client.SendAsync(request);

            // Endpoint returns JSON with an array of Group ObjectIDs
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                GroupResponse groups = JsonConvert.DeserializeObject<GroupResponse>(responseContent);

                // For each Group, add its Object ID to the ClaimsIdentity as a Group Claim
                foreach (string groupObjectID in groups.value)
                    claimsIdentity.AddClaim(new Claim("groups", groupObjectID, ClaimValueTypes.String, "AAD-Tenant-Security-Groups"));

                return groups.value;
            }
            else
            {
                throw new WebException();
            }
        }


        /// <summary>
        /// Checks to see if the signed-in user is an application owner. If so, assigns the user the application 
        /// role "Admin."  This is to ensure that at least one user can login and assign application roles to 
        /// other users the first time we run the app.
        /// </summary>
        /// <param name="claimsId">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <param name="userObjectId"> The ObjectId of the signed in user.</param>
        private void AddOwnerAdminClaim(string userObjectId, ClaimsIdentity claimsId)
        {
            // Acquire the Access Token
            ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
            AuthenticationResult result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
                new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = ConfigHelper.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

            FilterGenerator filter = new FilterGenerator();
            filter.QueryFilter = ExpressionHelper.CreateConditionalExpression(typeof(Application),
                GraphProperty.AppId, new Guid(ConfigHelper.ClientId), ExpressionType.Equal);
            PagedResults<Application> pagedApp = graphConnection.List<Application>(null, filter);

            PagedResults<GraphObject> owners = graphConnection.GetLinkedObjects(pagedApp.Results[0],
                LinkProperty.Owners, null);
            foreach (var owner in owners.Results)
            {
                if (owner.ObjectId == userObjectId)
                    claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
            }
            while (!owners.IsLastPage)
            {
                owners = graphConnection.GetLinkedObjects(pagedApp.Results[0], LinkProperty.Owners, owners.PageToken);
                foreach (var owner in owners.Results)
                {
                    if (owner.ObjectId == userObjectId)
                        claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
                }
            }
        }

        // These 3 classes are simply for Deserializing JSON
        // TODO: How to make reference from groups-->src1-->value
        private class ClaimSource
        {
            public Endpoint src1 { get; set; }

            public class Endpoint
            {
                public string endpoint { get; set; }
            }
        }

        private class GroupResponse
        {
            public string metadata { get; set; }
            public List<string> value { get; set; }
        }
        #endregion
    }
}