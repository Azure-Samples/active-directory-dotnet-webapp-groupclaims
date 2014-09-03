using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using WebAppRBACDotNet;
using WebAppRBACDotNet.Utils;

namespace RBACSampleADALv2.Controllers
{
    public class GroupTestController : Controller
    {
        // GET: GroupTest
        public ActionResult Index()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;

            //Get the Access Token for Calling Graph API from the cache
            AuthenticationResult result = null;
            try
            {
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
                RedirectToAction("Index", "Home", null);
            }

            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

            // Get Current User
            var user = graphConnection.Get<User>(userObjectId);

            for (int i = 0; i < 300; i++)
            {
                Group newGroup = new Group();
                newGroup.DisplayName = "Test Group " + i.ToString();
                newGroup.MailNickname = "TestGroup" + i.ToString();
                newGroup.MailEnabled = false;
                newGroup.SecurityEnabled = true;
                Group groupAdded = graphConnection.Add<Group>(newGroup);
                graphConnection.AddLink(groupAdded, user, LinkProperty.Members);
            }
            
            return RedirectToAction("Index", "Groups", null);
        }

        // GET: GroupTest
        public ActionResult Delete()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;

            //Get the Access Token for Calling Graph API from the cache
            AuthenticationResult result = null;
            try
            {
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
                RedirectToAction("Index", "Home", null);
            }

            // Setup Graph API connection
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

            // Query for the List of Security Groups.
            PagedResults<Group> pagedResults = graphConnection.List<Group>(null, null);
            foreach (Group group in pagedResults.Results)
            {
                if (group.DisplayName.Contains("Test"))
                    graphConnection.Delete(group);
            }
            while (!pagedResults.IsLastPage)
            {
                pagedResults = graphConnection.List<Group>(pagedResults.PageToken, null);
                foreach (Group group in pagedResults.Results)
                {
                    if (group.DisplayName.Contains("Test"))
                        graphConnection.Delete(group);
                }    
            }

            return RedirectToAction("Index", "Groups", null);
        }
    }
}