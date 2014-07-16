using System.Linq.Expressions;
using Microsoft.Ajax.Utilities;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using RBACSampleADALv2.Helpers;
using RBACSampleADALv2.Models;
using RBACSampleADALv2.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace RBACSampleADALv2.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var test = ClaimsPrincipal.Current;
            return View();
        }

        [Authorize]
        public ActionResult About()
        {
            List<string> myroles = new List<String>();
            List<string> mygroups = new List<String>();
            foreach (string str in RoleMapElem.Roles)
            {
                if (User.IsInRole(str))
                    myroles.Add(str);
            }
            ViewData["myroles"] = myroles;


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

            //Setup Graph Connection
            Guid clientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(result.AccessToken, clientRequestId, graphSettings);

            //Non-Batched Requests. In A Real App We Would Want to Batch Graph API requests
            foreach (var claim in ClaimsPrincipal.Current.FindAll("groups"))
            {
                try
                {
                    Group group = graphConnection.Get<Group>(claim.Value);
                    mygroups.Add(group.DisplayName);
                }
                catch { }
            }

            ViewData["mygroups"] = mygroups;
            return View();
        }
    }
}