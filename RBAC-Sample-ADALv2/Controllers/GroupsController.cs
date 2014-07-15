using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using RBACSampleADALv2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace RBACSampleADALv2.Controllers
{
    public class GroupsController : Controller
    {
        // GET: Groups
        [Authorize]
        [HttpGet]
        public ActionResult Index()
        {
           //Get the Access Token for Calling Graph API
            AuthenticationResult result = null;
            try
            {
                string tenantId = ClaimsPrincipal.Current.FindFirst(GraphConfiguration.TenantIdClaimType).Value;
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
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

            // Setup Graph API connection and get a list of users
            Guid ClientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

            PagedResults<Group> pagedReslts = graphConnection.List<Group>(null, new FilterGenerator());
            return View(pagedReslts.Results);
        }
        }
    }