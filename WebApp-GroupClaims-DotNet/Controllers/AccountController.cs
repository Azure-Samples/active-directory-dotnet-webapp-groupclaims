using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using WebApp_GroupClaims_DotNet.Models;
using WebApp_GroupClaims_DotNet.Utils;

namespace WebApp_GroupClaims_DotNet.Controllers
{
    public class AccountController : Controller
    {
        public void SignIn()
        {
            // Send an OpenID Connect sign-in request.
            if (!this.Request.IsAuthenticated)
                this.HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        /// <summary>
        /// Represents an event that is raised when the sign-out operation is complete.
        /// </summary>
        public void SignOut()
        {
            this.RemoveCachedTokens();

            string callbackUrl = this.Url.Action("SignOutCallback", "Account", routeValues: null, protocol: this.Request.Url.Scheme);

            // Send an OpenID Connect sign -out request.
            this.HttpContext.GetOwinContext().Authentication.SignOut(new AuthenticationProperties { RedirectUri = callbackUrl }, OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        /// <summary>
        /// Called by Azure AD. Here we end the user's session, but don't redirect to AAD for sign out.
        /// </summary>
        public void EndSession()
        {
            this.RemoveCachedTokens();

            this.HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult SignOutCallback()
        {
            if (this.Request.IsAuthenticated)
                return this.RedirectToAction("Index", "Home");

            return this.View();
        }

        /// <summary>
        /// Remove all cache entries for this user.
        /// </summary>
        private void RemoveCachedTokens()
        {
            if (this.Request.IsAuthenticated)
            {
                string signedInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                AuthenticationContext authContext = new AuthenticationContext(AppConfig.Authority, new ADALTokenCache(signedInUserId));
                authContext.TokenCache.Clear();
            }
        }
    }
}