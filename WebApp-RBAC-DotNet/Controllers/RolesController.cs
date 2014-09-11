using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using ExpressionHelper = Microsoft.Azure.ActiveDirectory.GraphClient.ExpressionHelper;

//The following libraries were defined and added to this sample.
using WebAppRBACDotNet.Models;
using WebAppRBACDotNet.Utils;
using RBACSampleADALv2.Utils;


namespace WebAppRBACDotNet.Controllers
{
    public class RolesController : Controller
    {
        ////////////////////////////////////////////////////////////////////
        //////// ACTIONS
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Show the current mappings of users and groups to each application role.
        /// Use AuthorizeAttribute to ensure only the role "Admin" can access the page.
        /// </summary>
        /// <returns>Role <see cref="View"/> with inputs to edit application role mappings.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            // Get Existing Mappings from Roles.xml
            ViewBag.Message = "RoleMappings";
            List<RoleMapping> mappings = DbAccess.GetAllRoleMappings();

            //Dictionary of <ObjectID, DisplayName> pairs
            var nameDict = new Dictionary<string, string>();

            //Get the Access Token for Calling Graph API
            AuthenticationResult result = null;
            try
            {
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                var authContext = new AuthenticationContext(Startup.Authority,
                    new TokenDbCache(userObjectId));
                var credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                result = authContext.AcquireTokenSilent(GraphConfiguration.GraphResourceId, credential,
                    new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            }
            catch (Exception e)
            {
                // If the user doesn't have an access token, they need to re-authorize

                // If refresh is set to true, the user has clicked the link to be authorized again.
                if (Request.QueryString["reauth"] == "True")
                {

                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.

                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                // The user needs to re-authorize.  Show them a message to that effect.
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Construct the <ObjectID, DisplayName> Dictionary, Add Lists of Mappings to ViewData
            // for each type of role
            foreach (RoleMapping mapping in mappings)
                nameDict[mapping.ObjectId] = GetDisplayNameFromObjectId(result.AccessToken, mapping.ObjectId);
            

            //TODO: Pass Lists of Mappings to View For Presentation (RoleList thing)
            ViewData["mappings"] = mappings;
            ViewData["nameDict"] = nameDict;
            ViewData["roles"] = Globals.Roles;
            return View();
        }


        /// <summary>
        /// Adds a User/Group<-->Application Role mapping from user input form
        /// to roles.xml if it does not already exist.
        /// </summary>
        /// <param name="formCollection">The user input form, containing the UPN or GroupName
        /// of the object to grant a role.</param>
        /// <returns>A Redirect to the Roles page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult AssignRole(FormCollection formCollection)
        {
            // Check for an input name
            if (formCollection != null && formCollection["name"].Length > 0)
            {
                //Get the Access Token for Calling Graph API from the cache
                AuthenticationResult result = null;
                try
                {
                    string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                    var authContext = new AuthenticationContext(Startup.Authority,
                        new TokenDbCache(userObjectId));
                    var credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                    result = authContext.AcquireTokenSilent(GraphConfiguration.GraphResourceId, credential,
                        new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                }
                catch (Exception e)
                {
                    // The user needs to re-authorize.  Show them a message to that effect.
                    return RedirectToAction("Index", "Roles", null);
                }

                // Get ObjectID of User Or Group from Name provided by user
                string objectId = GetObjectIDFromDisplayNameOrUPN(result.AccessToken, formCollection["name"]);

                // If object DNE, show an error
                if (objectId == null)
                    return RedirectToAction("ShowError", "Error", new { errorMessage = "User/Group Not Found." });

                DbAccess.AddRoleMapping(objectId, formCollection["roletype"]);
            }

            return RedirectToAction("Index", "Roles", null);
        }


        /// <summary>
        /// Removes a ObjectID<-->Application Role mapping from Roles.xml, based on input
        /// from the user.
        /// </summary>
        /// <param name="formCollection">The input from the user.</param>
        /// <returns>A redirect to the roles page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult RemoveRole(FormCollection formCollection)
        {
            // Remove role mapping assignments marked by checkboxes
            foreach (string key in formCollection.Keys)
            {
                if (formCollection[key].Equals("delete"))
                    DbAccess.RemoveRoleMapping(Convert.ToInt32(key));
            }
            return RedirectToAction("Index", "Roles", null);
        }

        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queries the GraphAPI to get the DisplayName of a Group or User from its ObjectID.
        /// </summary>
        /// <param name="accessToken">The OpenIDConnect access token used 
        /// to query the GraphAPI</param>
        /// <param name="objectId">The ObjectID of the User or Group</param>
        /// <returns>The DisplayName.</returns>
        private static string GetDisplayNameFromObjectId(string accessToken, string objectId)
        {
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            try
            {
                // Get a User by ObjectID
                return graphConnection.Get<User>(objectId).DisplayName;
            }
            catch
            {
                try
                {
                    // If the User with ObjectID DNE, Get a group with the ObjectID
                    return graphConnection.Get<Group>(objectId).DisplayName;
                }
                catch
                {
                    try
                    {
                        // If the User and Group with ObjectID, Get a Built-In Directory Role
                        return graphConnection.Get<Role>(objectId).DisplayName;
                    }
                    catch
                    {
                        // If neither a User nor a Group nor a Role was found, return null
                        return null;
                    }
                }
            }
        }


        /// <summary>
        /// Queries the GraphAPI to get the ObjectID of a group or user from their name or UPN, respectively.
        /// </summary>
        /// <param name="accessToken">The OpenIDConnect access token used 
        /// to query the GraphAPI</param>
        /// <param name="name">The Display Name or UPN of the group or user, respectively.</param>
        /// <returns>The ObjectID.</returns>
        private static string GetObjectIDFromDisplayNameOrUPN(string accessToken, string name)
        {
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            // First, search GraphAPI for a User with corresponding UPN
            var filter = new FilterGenerator();
            filter.QueryFilter =
                ExpressionHelper.CreateConditionalExpression(
                    typeof(User), GraphProperty.UserPrincipalName, name, ExpressionType.Equal);
            PagedResults<User> pagedUserResults = graphConnection.List<User>(null, filter);

            // If found, return User ObjectID
            if (pagedUserResults.Results != null && pagedUserResults.Results.Count > 0)
            {
                return pagedUserResults.Results[0].ObjectId;
            }

            // If not found, search GraphAPI for a Group with corresponding DisplayName
            filter.QueryFilter = ExpressionHelper.CreateConditionalExpression(
                typeof(Group), GraphProperty.DisplayName, name, ExpressionType.Equal);
            PagedResults<Group> pagedGroupResults = graphConnection.List<Group>(null, filter);

            // If found, return Group objectID
            if (pagedGroupResults.Results != null && pagedGroupResults.Results.Count > 0)
            {
                return pagedGroupResults.Results[0].ObjectId;
            }

            // If object DNE, return null
            return null;
        }
    }
}