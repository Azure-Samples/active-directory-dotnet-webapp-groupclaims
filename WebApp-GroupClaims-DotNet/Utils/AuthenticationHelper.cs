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
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp_GroupClaims_DotNet.Models;
using ClaimTypes = System.IdentityModel.Claims.ClaimTypes;

namespace WebApp_GroupClaims_DotNet.Utils
{
    public class AuthenticationHelper
    {
        public const string OAuth2GrantTypeJwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        public TokenCache TokenCache { get; set; }

        public string Authority { get; set; }

        public AuthenticationHelper(string authority, TokenCache tokenCache)
        {
            this.Authority = authority;
            this.TokenCache = tokenCache;
        }
        
        public async Task<String> GetOnBehalfOfAccessToken(string resourceId, string replyUrl)
        {
            string accessToken = null;
            AuthenticationResult result = null;

            //      The Resource ID of the service we want to call.
            //      The current user's access token, from the current request's authorization header.
            //      The credentials of this application.
            //      The username (UPN or email) of the user calling the API
            //
            ClientCredential clientCred = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);

            if (ClaimsPrincipal.Current.Identities.First().BootstrapContext == null)
            {
                throw new Exception("BootstrapContext is null. Please modify the config 'saveSignInToken = true' to ensure that the original token is preserved.");
            }

            System.IdentityModel.Tokens.BootstrapContext bootstrapContext = new System.IdentityModel.Tokens.BootstrapContext(ClaimsPrincipal.Current.Identities.First().BootstrapContext.ToString());
            if (bootstrapContext == null || string.IsNullOrWhiteSpace(bootstrapContext.Token))
            {
                throw new Exception("Failed to obtain the BootstrapContext token. Please modify the config 'saveSignInToken = true' to ensure that the original token is preserved.");
            }

            string userAccessToken = bootstrapContext.Token;

            // The username (UPN or email) of the user calling the API
            string userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn) != null ? ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value : ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email).Value;
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new Exception("Failed to obtain the signed-in user's name or upn in the ClaimsPrincipal.Current.Claims collection.");
            }

            UserAssertion userAssertion = new UserAssertion(userAccessToken, OAuth2GrantTypeJwtBearer, userName);

            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            AuthenticationContext authContext = new AuthenticationContext(this.Authority, this.TokenCache);

            try
            {
                result = await authContext.AcquireTokenSilentAsync(resourceId, clientCred, new UserIdentifier(Util.GetSignedInUsersObjectIdFromClaims(), UserIdentifierType.UniqueId));
            }
            catch(AdalException ex)
            {
                result = await authContext.AcquireTokenAsync(resourceId, clientCred, userAssertion);
            }

            accessToken = result.AccessToken;
            return accessToken;
        }

        public async Task<string> GetAccessTokenForAppAsync(string resourceId)
        {
            AuthenticationContext authContext = new AuthenticationContext(this.Authority);
            AuthenticationResult authResult = null;
            ClientCredential creds = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);

            try
            {
                authResult = await authContext.AcquireTokenAsync(resourceId, creds);
            }
            catch (AdalException ex)
            {
                throw ex;
            }

            return authResult.AccessToken;
        }
    }
}