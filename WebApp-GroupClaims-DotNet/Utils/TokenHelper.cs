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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;
using WebApp_GroupClaims_DotNet.Models;

namespace WebApp_GroupClaims_DotNet.Utils
{
    public class TokenHelper
    {
        public static async Task<UserGroupsAndDirectoryRoles> GetUsersGroupsAsync(ClaimsPrincipal subject)
        {
            UserGroupsAndDirectoryRoles userGroupsAndDirectoryRoles = new UserGroupsAndDirectoryRoles();
            userGroupsAndDirectoryRoles.HasOverageClaim = HasGroupsOverageClaim(subject);
            ClaimsIdentity userClaimsId = subject.Identity as ClaimsIdentity;

            if (userGroupsAndDirectoryRoles.HasOverageClaim)
            {
                userGroupsAndDirectoryRoles.GroupIds.AddRange(await GetUsersGroupsFromClaimSourcesAsync(userClaimsId));
            }
            else
            {
                userGroupsAndDirectoryRoles.GroupIds.AddRange(userClaimsId.FindAll(SubjectAttribute.Groups).Select(c => c.Value).ToList());
            }

            return userGroupsAndDirectoryRoles;
        }

        private static async Task<IList<string>> GetUsersGroupsFromClaimSourcesAsync(ClaimsIdentity claimsIdentity)
        {
            List<string> groupObjectIds = new List<string>();
            ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);

            // Acquire the Access Token for AAD graph has the claim source still points to AAD graph
            AuthenticationHelper authHelper = new AuthenticationHelper(ConfigHelper.Authority, new ADALTokenCache(Util.GetSignedInUsersObjectIdFromClaims()));
            var token = await authHelper.GetOnBehalfOfAccessToken(ConfigHelper.AADGraphResourceId, ConfigHelper.PostLogoutRedirectUri);
                      
            // Get the GraphAPI Group Endpoint for the specific user from the _claim_sources claim in token
            string groupsClaimSourceIndex = (Json.Decode(claimsIdentity.FindFirst("_claim_names").Value)).groups;
            var groupClaimsSource = (Json.Decode(claimsIdentity.FindFirst("_claim_sources").Value))[groupsClaimSourceIndex];
            string requestUrl = groupClaimsSource.endpoint + "?api-version=1.5";

            // Prepare and Make the POST request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            StringContent content = new StringContent("{\"securityEnabledOnly\": \"true\"}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
            HttpResponseMessage response = await client.SendAsync(request);

            // Endpoint returns JSON with an array of Group ObjectIDs
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var groupsResult = (Json.Decode(responseContent)).value;

                foreach (string groupObjectID in groupsResult)
                    groupObjectIds.Add(groupObjectID);
            }
            else
            {
                throw new WebException();
            }

            return groupObjectIds;
        }

        internal static bool HasGroupsOverageClaim(ClaimsPrincipal subject)
        {
            Claim claimNames = subject.FindFirst(SubjectAttribute.ClaimNames);

            if (claimNames != null)
            {
                return (Json.Decode(claimNames.Value).groups != null);
            }

            return false;
        }
    }
}