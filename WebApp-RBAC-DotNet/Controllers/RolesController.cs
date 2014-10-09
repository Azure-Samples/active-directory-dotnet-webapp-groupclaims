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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;


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
            AuthenticationContext authContext;
            AuthenticationResult result = null;
            try
            {
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                authContext = new AuthenticationContext(Globals.Authority,
                    new TokenDbCache(userObjectId));
                var credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                result = authContext.AcquireTokenSilent(Globals.GraphResourceId, credential,
                    new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            }
            catch (AdalException e)
            {
                // If the user doesn't have an access token, they need to re-authorize
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                {
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

                return RedirectToAction("Show Error", "Error", new { errorMessage = "Error while acquiring token." });
            }

            // Construct the <ObjectID, DisplayName> Dictionary, Add Lists of Mappings to ViewData
            // for each type of role
            foreach (RoleMapping mapping in mappings)
            {
                try
                {
                    nameDict[mapping.ObjectId] = GetDisplayNameFromObjectId(result.AccessToken, mapping.ObjectId);
                }
                catch (GraphException e)
                {
                    if (e.HttpStatusCode == HttpStatusCode.Unauthorized)
                    {
                        // The user needs to re-authorize.  Show them a message to that effect.
                        authContext.TokenCache.Clear();
                        ViewBag.ErrorMessage = "AuthorizationRequired";
                        return View();
                    }
                    return RedirectToAction("Show Error", "Error", new { errorMessage = "Error while calling Graph API." });
                }
            }
            
            ViewData["mappings"] = mappings;
            ViewData["nameDict"] = nameDict;
            ViewData["roles"] = Globals.Roles;
            ViewData["host"] = Request.Url.AbsoluteUri;
            ViewData["token"] = result.AccessToken;
            ViewData["tenant"] = Globals.Tenant;
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
            if (formCollection != null && formCollection["id"].Length > 0)
            {
                //Get the Access Token for Calling Graph API from the cache
                AuthenticationResult result = null;
                try
                {
                    string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                    var authContext = new AuthenticationContext(Globals.Authority,
                        new TokenDbCache(userObjectId));
                    var credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                    result = authContext.AcquireTokenSilent(Globals.GraphResourceId, credential,
                        new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                }
                catch (AdalException e)
                {
                        // The user needs to re-authorize.  Show them a message to that effect.
                        return RedirectToAction("Index", "Roles", null);  
                }

                DbAccess.AddRoleMapping(formCollection["id"], formCollection["role"]);
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

        /// <summary>
        /// Used for the AadPickerLibrary that is used to search for users and groups.  Accepts a user input
        /// and a number of results to retreive, and queries the graphAPI for possbble matches.
        /// </summary>
        /// <returns>JSON containing query results ot the Javascript library.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async System.Threading.Tasks.Task<ActionResult> Search(string query, string token)
        {
            // Search for users based on user input.
            try
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return this.Content(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Json(new { error = "graph api error" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { error = "internal server error" }, JsonRequestBehavior.AllowGet);
            }
        }

        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////
        #region HelperFunctions
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
            graphSettings.ApiVersion = Globals.GraphApiVersion;
            var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            try
            {
                // Get a User by ObjectID
                return graphConnection.Get<User>(objectId).DisplayName;
            }
            catch (GraphException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    try
                    {
                        // If the User with ObjectID DNE, Get a group with the ObjectID
                        return graphConnection.Get<Group>(objectId).DisplayName;
                    }
                    catch (GraphException eprime)
                    {
                        if (eprime.HttpStatusCode == HttpStatusCode.NotFound)
                        {
                            try
                            {
                                // If the User and Group with ObjectID, Get a Built-In Directory Role
                                return graphConnection.Get<Role>(objectId).DisplayName;
                            }
                            catch (GraphException eprimeprime)
                            {
                                if (eprimeprime.HttpStatusCode == HttpStatusCode.NotFound)
                                {
                                    // If neither a User nor a Group nor a Role was found, return null
                                    return null;
                                }
                                else { throw eprimeprime; }
                            }
                        }
                        else { throw eprime; }
                    }
                }
                else { throw e; }
            }
        }
        #endregion
    }
}