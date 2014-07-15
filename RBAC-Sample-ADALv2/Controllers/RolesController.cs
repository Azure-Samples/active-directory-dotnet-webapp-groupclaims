using RBACSampleADALv2.Helpers;
using RBACSampleADALv2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using RBACSampleADALv2.Utils;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Linq.Expressions;
using System.IdentityModel.Tokens;

namespace RBACSampleADALv2.Controllers
{
    public class RolesController : Controller
    {
        ////////////////////////////////////////////////////////////////////
        //////// ACTIONS
        ////////////////////////////////////////////////////////////////////

        // GET: Roles
        // Use Authorize attribute to allow only Admins to change role mappings
        [HttpGet]
        [Authorize(Roles = "Admin")] //TODO: Bug on Github Sample (redirect loop)
        public ActionResult Index()
        {
            //Get the Mappings from XML file
            ViewBag.Message = "RoleMappings";
            List<List<RoleMapElem>> mappings = XmlHelper.GetRoleMappingsFromXml();

            //Dictionary of <ObjectID, Name> pairs
            Dictionary<string, string> nameDict = new Dictionary<string, string>();

            //Get the Access Token for Calling Graph API
            AuthenticationResult result = null;
            try
            {
                string tenantId = ClaimsPrincipal.Current.FindFirst(GraphConfiguration.TenantIdClaimType).Value;
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectId));
                ClientCredential credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                result = authContext.AcquireTokenSilent(GraphConfiguration.GraphResourceId, credential,
                    new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            }
            catch (Exception e)
            {
                //if the user doesn't have an access token, they need to re-authorize

                // If refresh is set to true, the user has clicked the link to be authorized again.
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                // The user needs to re-authorize.  Show them a message to that effect.
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            //Construct the ObjectID-->Name Dictionary, Add Lists of Mappings to ViewData
            //for each type of role
            for (int i = 0; i < mappings.Count; i++)
            {
                //for each mapping entry in that role
                for (int j = 0; j < mappings[i].Count; j++)
                {
                    nameDict[mappings[i][j].ObjectId] = GetDisplayNameFromObjectId(result.AccessToken, mappings[i][j].ObjectId);
                }
                //Ex: ViewData["AdminList"] = List<RoleMapElem>
                ViewData[RoleMapElem.Roles[i] + "List"] = mappings[i];
            }

            ViewData["nameDict"] = nameDict;
            ViewData["roles"] = RoleMapElem.Roles;
            return View();

        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public ActionResult AssignRole(FormCollection formCollection)
        {
            //add new role mapping assignment
            if (formCollection != null && formCollection["name"].Length > 0)
            {
                //Get the Access Token for Calling Graph API
                AuthenticationResult result = null;
                try
                {
                    string tenantId = ClaimsPrincipal.Current.FindFirst(GraphConfiguration.TenantIdClaimType).Value;
                    string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                    Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(Startup.Authority,
                        new NaiveSessionCache(userObjectId));
                    ClientCredential credential = new ClientCredential(Globals.ClientId, Globals.AppKey);
                    result = authContext.AcquireTokenSilent(GraphConfiguration.GraphResourceId, credential,
                        new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                }
                catch (Exception e)
                {
                    //if the user doesn't have an access token, they need to re-authorize

                    // If refresh is set to true, the user has clicked the link to be authorized again.
                    if (Request.QueryString["reauth"] == "True")
                    {
                        //
                        // Send an OpenID Connect sign-in request to get a new set of tokens.
                        // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                        // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                        //
                        HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                    }

                    // The user needs to re-authorize.  Show them a message to that effect.
                    ViewBag.ErrorMessage = "AuthorizationRequired";
                    return View();
                }

                // Setup Graph API connection
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

                //Search Graph API for Users by UPN
                FilterGenerator filter = new FilterGenerator();
                filter.QueryFilter =
                    Microsoft.Azure.ActiveDirectory.GraphClient.ExpressionHelper.CreateConditionalExpression(
                        typeof(User), GraphProperty.UserPrincipalName, formCollection["name"], ExpressionType.Equal);
                PagedResults<User> pagedUserResults = graphConnection.List<User>(null, filter);

                //If found, get objectID
                string objectId = null;
                if (pagedUserResults.Results != null && pagedUserResults.Results.Count > 0)
                {
                    objectId = pagedUserResults.Results[0].ObjectId;
                }
                //If not found, search GraphAPI for Groups by Group DisplayName
                if (objectId == null)
                {
                    filter.QueryFilter = Microsoft.Azure.ActiveDirectory.GraphClient.ExpressionHelper.CreateConditionalExpression(
                        typeof(Group), GraphProperty.DisplayName, formCollection["name"], ExpressionType.Equal);
                    PagedResults<Group> pagedGroupResults = graphConnection.List<Group>(null, filter);

                    //If found, get objectID
                    if (pagedGroupResults.Results != null && pagedGroupResults.Results.Count > 0)
                    {
                        objectId = pagedGroupResults.Results[0].ObjectId;
                    }
                }
                //If object DNE, show an error
                if (objectId == null)
                {
                    return RedirectToAction("ShowError", "Error", new { errorMessage = "User/Group Not Found." });
                }
                XmlHelper.AppendRoleMappingToXml(formCollection, objectId);
            }

            return RedirectToAction("Index", "Roles", null);
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public ActionResult RemoveRole(FormCollection formCollection)
        {
            //remove role mapping assignments marked by checkboxes
            XmlHelper.RemoveRoleMappingsFromXml(formCollection);

            return RedirectToAction("Index", "Roles", null);
        }

        ////////////////////////////////////////////////////////////////////
        //////// HELPER FUNCTIONS
        ////////////////////////////////////////////////////////////////////

        private static string GetDisplayNameFromObjectId(string accessToken, string objectId)
        {
            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            try
            {
                //Try to get a User by ObjectID
                User user = graphConnection.Get<User>(objectId);
                return user.DisplayName;

            }
            catch
            {
                try
                {
                    //Try to get a Group by ObjectID
                    Group group = graphConnection.Get<Group>(objectId);
                    return group.DisplayName;
                }
                catch
                {
                    return null;
                }
            }
        }
    }   
}