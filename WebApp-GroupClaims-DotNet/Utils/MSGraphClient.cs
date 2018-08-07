using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;

namespace WebApp_GroupClaims_DotNet.Utils
{
    public class MSGraphClient
    {
        private GraphServiceClient graphServiceUserClient;
        private GraphServiceClient graphServiceClient;
        private readonly AuthenticationHelper authHelper;

        public TokenCache TokenCache { get; set; }

        public string Authority { get; set; }

        public MSGraphClient(string authority, TokenCache tokenCache)
        {
            this.Authority = authority;
            this.TokenCache = tokenCache;
            this.authHelper = new AuthenticationHelper(this.Authority, this.TokenCache);
        }

        public async Task<User> GetMeAsync()
        {
            User currentUserObject;

            try
            {
                GraphServiceClient graphClient = this.GetAuthenticatedClientForUser();
                currentUserObject = await graphClient.Me.Request().GetAsync();

                if (currentUserObject != null)
                {
                    Trace.WriteLine($"Got user: {currentUserObject.DisplayName}");
                }
            }
            catch (ServiceException e)
            {
                Trace.Fail($"We could not get the current user details: {e.Error.Message}");
                return null;
            }

            return currentUserObject;
        }

        private GraphServiceClient GetAuthenticatedClientForUser()
        {
            if (this.graphServiceUserClient == null)
            {
                string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                // Create Microsoft Graph client.
                try
                {
                    this.graphServiceUserClient = new GraphServiceClient(AppConfig.MSGraphBaseUrl,
                                                                         new DelegateAuthenticationProvider(
                                                                             async (requestMessage) =>
                                                                             {
                                                                                 var token = await this.authHelper.GetAccessTokenForUserAsync(AppConfig.GraphResourceId, AppConfig.PostLogoutRedirectUri, signedInUserID);
                                                                                 requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                                                                             }));
                }
                catch (Exception ex)
                {
                    Trace.Fail($"Could not create a graph client {ex}");
                }
            }

            return this.graphServiceUserClient;
        }

        private GraphServiceClient GetAuthenticatedClientForApp()
        {
            if (this.graphServiceClient == null)
            {
                // Create Microsoft Graph client.
                try
                {
                    this.graphServiceClient = new GraphServiceClient(AppConfig.MSGraphBaseUrl,
                                                                     new DelegateAuthenticationProvider(
                                                                         async (requestMessage) =>
                                                                         {
                                                                             var token = await this.authHelper.GetAccessTokenForAppAsync(AppConfig.GraphResourceId);
                                                                             requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                                                                         }));
                }
                catch (Exception ex)
                {
                    Trace.Fail($"Could not create a graph client {ex}");
                }
            }

            return this.graphServiceClient;
        }
    }
}