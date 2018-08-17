using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.IdentityModel.Claims;
using System.Web;
using WebApp_GroupClaims_DotNet.Models;
using WebApp_GroupClaims_DotNet.Utils;

namespace WebApp_GroupClaims_DotNet
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            //Configure OpenIDConnect, register callbacks for OpenIDConnect Notifications
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = AppConfig.ClientId,
                    Authority = AppConfig.Authority,
                    RedirectUri = "https://localhost:44322/",
                    PostLogoutRedirectUri = AppConfig.PostLogoutRedirectUri,

                    TokenValidationParameters = new TokenValidationParameters
                    {
                        SaveSigninToken = true
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        AuthorizationCodeReceived = (context) =>
                        {
                            var code = context.Code;
                            ClientCredential credential = new ClientCredential(AppConfig.ClientId, AppConfig.AppKey);
                            string userObjectId = context.AuthenticationTicket.Identity.FindFirst(Globals.ObjectIdClaimType).Value;
                            AuthenticationContext authContext = new AuthenticationContext(AppConfig.Authority, new ADALTokenCache(userObjectId));

                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCodeAsync(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, AppConfig.GraphResourceId).Result;

                            return System.Threading.Tasks.Task.FromResult(0);
                        },

                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            return System.Threading.Tasks.Task.FromResult(0);
                        }
                    }
                });
        }
    }
}