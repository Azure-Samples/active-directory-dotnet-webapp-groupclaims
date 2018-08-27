/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
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
                AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new ADALTokenCache(Util.GetSignedInUsersObjectIdFromClaims()));
                authContext.TokenCache.Clear();
            }
        }
    }
}