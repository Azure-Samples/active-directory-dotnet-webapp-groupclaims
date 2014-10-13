using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;

//The following libraries were defined and added to this sample.
using WebAppGroupClaimsDotNet.Models;
using WebAppGroupClaimsDotNet.Utils;
using System.Net;

namespace WebAppGroupClaimsDotNet.Controllers
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
            AuthenticationContext authContext;

            // Check if the user has been granted each application role.
            foreach (string str in Globals.Roles)
            {
                if (User.IsInRole(str))
                    myroles.Add(str);
            }
            ViewData["myroles"] = myroles;
            ViewData["mygroups"] = mygroups;


            //Get the Access Token for Calling Graph API frome the cache
            AuthenticationResult result = null;
            try
            {
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                authContext = new AuthenticationContext(ConfigHelper.Authority,
                    new TokenDbCache(userObjectId));
                var credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
                    new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            }
            catch (AdalException e)
            {
                // If the user doesn't have an access token, they need to re-authorize
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                        return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while acquiring token." });
            }

            // Setup Graph Connection
            Guid clientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = ConfigHelper.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, clientRequestId, graphSettings);

            Dictionary<string, string> groupNameDict = new Dictionary<string, string>();

            try { 
                // For each Group Claim, we need to get the DisplayName of the Group from the GraphAPI
                // We choose to iterate over the set of all groups rather than query the GraphAPI for each group.
                // First, put all <GroupObjectID, DisplayName> pairs into a dictionary

                PagedResults<Group> pagedResults = graphConnection.List<Group>(null, null);
                foreach (Group group in pagedResults.Results)
                    groupNameDict[group.ObjectId] = group.DisplayName;
                while (!pagedResults.IsLastPage)
                {
                    pagedResults = graphConnection.List<Group>(pagedResults.PageToken, null);
                    foreach (Group group in pagedResults.Results)
                        groupNameDict[group.ObjectId] = group.DisplayName;
                }
            }
            catch (GraphException e) {

                if (e.HttpStatusCode == HttpStatusCode.Unauthorized) {
                    // The user needs to re-authorize.  Show them a message to that effect.
                    authContext.TokenCache.Clear();
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                }

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while calling Graph API." });
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