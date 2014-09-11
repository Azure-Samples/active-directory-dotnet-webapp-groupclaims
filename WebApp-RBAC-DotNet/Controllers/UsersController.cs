using System;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;

//The following libraries were defined and added to this sample.
using WebAppRBACDotNet.Utils;
using RBACSampleADALv2.Utils;

namespace WebAppRBACDotNet.Controllers
{
    public class UsersController : Controller
    {
        /// <summary>
        /// Lists Out All Users in the Application by Querying the GraphAPI.
        /// </summary>
        /// <returns>The Users Page.</returns>
        [HttpGet]
        [Authorize]
        public ActionResult Index()
        {
            List<User> users = new List<User>();

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
                //if the user doesn't have an access token, they need to re-authorize

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
                return View(users);
            }

            
            // Setup Graph API connection and get a list of users
            Guid ClientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            var graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

            //Query the GraphAPI for all users and add to a single list.
            PagedResults<User> pagedResults = graphConnection.List<User>(null, null);
            users.AddRange(pagedResults.Results);
            while (!pagedResults.IsLastPage)
            {
                pagedResults = graphConnection.List<User>(pagedResults.PageToken, null);
                users.AddRange(pagedResults.Results);
            }

            return View(users);
        }
    }
}