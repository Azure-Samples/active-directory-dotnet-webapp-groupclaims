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
                    ClientId = Globals.ClientId,
                    Authority = Globals.Authority,
                    PostLogoutRedirectUri = Globals.PostLogoutRedirectUri,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        // Redeem an Authorization Code recieved in an OpenIDConnect message for an Access Token & Refresh Token, and store them away.
                        AuthorizationCodeReceived = context =>
                        {
                            try
                            {
                                ClaimsIdentity claimsId = context.AuthenticationTicket.Identity;
                                ClientCredential credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                                string userObjectId = claimsId.FindFirst(Globals.ObjectIdClaimType).Value;
                                AuthenticationContext authContext = new AuthenticationContext(Globals.Authority, new TokenDbCache(userObjectId));
                                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                    context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, Globals.GraphResourceId);

                                // Get the list of groups the user is a member of, and assign roles accordingly.
                                List<String> groupMemberships = GetGroupsFromGraphAPI(result.AccessToken, claimsId, userObjectId);
                                AssignRoles(userObjectId, groupMemberships, claimsId);

                                // Assign Admin priviliges to the AAD Application Owners, to bootstrap the application on first run.
                                AddOwnerMappings(userObjectId, result.AccessToken, claimsId);

                                return System.Threading.Tasks.Task.FromResult(0);
                            }
                            catch (AdalException e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                                return System.Threading.Tasks.Task.FromResult(0);
                            }
                            catch (GraphException e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                                return System.Threading.Tasks.Task.FromResult(0);
                            }
                        }
                    }
                });
        }


        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////
        #region HelperFunctions
        /// <summary>
        /// Assign the application roles (Owner, Admin, Writer, Approver, Observer) that the user has been granted,
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
                { 
                    if (mapping.Role != "Owner")
                        claimsId.AddClaim(new Claim(ClaimTypes.Role, mapping.Role, ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
                }
            }
        }

        /// <summary>
        /// We must query the GraphAPI to obtain information about the user and the security groups they are a member of.
        /// Here we use the GraphAPI Client Library to do so.
        /// </summary>
        /// <param name="accessToken">The OpenIDConnect access token, used here for querying the GraphAPI.</param>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <returns>A list of ObjectIDs representing the groups that the user is a member of.</returns>
        private List<String> GetGroupsFromGraphAPI(string accessToken, ClaimsIdentity claimsIdentity, string userObjectId)
        {
            var pagedResults = new PagedResults<GraphObject>();
            var builtInRolesAndGroups = new List<GraphObject>();
            var listOfGroupObjectIDs = new List<String>();

            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = Globals.GraphApiVersion;
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

            // For each object returned by the GraphAPI
            foreach (GraphObject roleOrGroup in builtInRolesAndGroups)
                listOfGroupObjectIDs.Add(roleOrGroup.ObjectId);

            return listOfGroupObjectIDs;
        }


        /// <summary>
        /// Checks to see if the signed-in user is an application owner. If so, assigns the user the application 
        /// role "Owner" in the database and grants them "Admin" priviliges.  This is to ensure that
        /// at least one user can login and assign application roles to other users the first time we run the app.
        /// There are several other ways to accomplish this.
        /// </summary>
        /// <param name="accessToken">The Access token, used here to query the GraphAPI.</param>
        /// <param name="claimsId">The <see cref="ClaimsIdenity" /> object that represents the 
        /// claims-based identity of the currently signed in user and contains thier claims.</param>
        /// <param name="userObjectId"> The ObjectId of the signed in user.</param>
        private void AddOwnerMappings(string userObjectId, string accessToken, ClaimsIdentity claimsId)
        {
            // Every time a user signs in, we rewrite the list of owners in case owners have been removed in the Azure Portal.
            DbAccess.RemoveExistingOwnerMappings();
            
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = Globals.GraphApiVersion;
            var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            FilterGenerator filter = new FilterGenerator();
            filter.QueryFilter = ExpressionHelper.CreateConditionalExpression(typeof(Application),
                GraphProperty.AppId, new Guid(Globals.ClientId), ExpressionType.Equal);
            PagedResults<Application> pagedApp = graphConnection.List<Application>(null, filter);

            PagedResults<GraphObject> owners = graphConnection.GetLinkedObjects(pagedApp.Results[0],
                LinkProperty.Owners, null);
            foreach (var owner in owners.Results)
            {
                DbAccess.AddRoleMapping(owner.ObjectId, "Owner");
                if (owner.ObjectId == userObjectId)
                    claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
            }
            while (!owners.IsLastPage)
            {
                owners = graphConnection.GetLinkedObjects(pagedApp.Results[0], LinkProperty.Owners, owners.PageToken);
                foreach (var owner in owners.Results)
                {
                    DbAccess.AddRoleMapping(owner.ObjectId, "Owner");
                    if (owner.ObjectId == userObjectId)
                        claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebAppGroupClaimsDotNet"));
                }
            }
        }
        #endregion
    }
}