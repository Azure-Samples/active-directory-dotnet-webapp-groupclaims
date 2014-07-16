using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using RBACSampleADALv2.Models;
using RBACSampleADALv2.Utils;

namespace RBACSampleADALv2.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Shows the generic MVC Get Started Home Page. Allows unauthenticated
        /// users to see the home page and click the sign-in link.
        /// </summary>
        /// <returns>Generic Home <see cref="View"/>.</returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Gets user specific RBAC information: The Security Groups the user belongs to
        /// And the application roles the user has been granted.
        /// </summary>
        /// <returns>The About <see cref="View"/>.</returns>
        [Authorize]
        public ActionResult About()
        {
            var myroles = new List<String>();
            var mygroups = new List<String>();

            // Check if the user has been granted each application role.
            foreach (string str in RoleMapElem.Roles)
            {
                if (User.IsInRole(str))
                    myroles.Add(str);
            }
            ViewData["myroles"] = myroles;


            //Get the Access Token for Calling Graph API frome the cache
            AuthenticationResult result = null;
            try
            {
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                var authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectId));
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

            // Setup Graph Connection
            Guid clientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, clientRequestId, graphSettings);

            // For each Group Claim, we need to get the DisplayName of the Group from the GraphAPI
            // We choose to iterate over the set of all groups rather than query the GraphAPI multiple times.
            // First, put all <GroupObjectID, DisplayName> pairs into a dictionary
            Dictionary<string, string> groupNameDict = new Dictionary<string, string>();
            PagedResults<Group> pagedResults = graphConnection.List<Group>(null, null);
            foreach (Group group in pagedResults.Results)
                groupNameDict[group.ObjectId] = group.DisplayName;
            while (!pagedResults.IsLastPage)
            {
                pagedResults = graphConnection.List<Group>(pagedResults.PageToken, null);
                foreach (Group group in pagedResults.Results)
                    groupNameDict[group.ObjectId] = group.DisplayName;
            }

            // For the security groups the user is a member of, get the DisplayName
            foreach (Claim claim in ClaimsPrincipal.Current.FindAll("groups"))
            {
                string displayName;
                if (groupNameDict.TryGetValue(claim.Value, out displayName))
                    mygroups.Add(displayName);
            }

            ViewData["mygroups"] = mygroups;
            return View();
        }
    }
}