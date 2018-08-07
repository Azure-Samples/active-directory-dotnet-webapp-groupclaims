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

        /// <summary>
        /// Calls the Graph /me endpoint using OBO.
        /// </summary>
        /// <returns></returns>
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

        public async Task<IList<Group>> GetCurrentUserGroupsAsync()
        {
            IUserMemberOfCollectionWithReferencesPage memberOfGroups = null;
            IList<Group> groups = new List<Group>();

            try
            {
                GraphServiceClient graphClient = this.GetAuthenticatedClientForUser();
                memberOfGroups = await graphClient.Me.MemberOf.Request().GetAsync();
                
                if(memberOfGroups != null)
                {
                    do
                    {
                        foreach (var directoryObject in memberOfGroups.CurrentPage)
                        {
                            if (directoryObject is Group)
                            {
                                Group group = directoryObject as Group;
                                Trace.WriteLine("Got group: " + group.Id);
                                groups.Add(group as Group);
                            }
                        }
                        if (memberOfGroups.NextPageRequest != null)
                        {
                            memberOfGroups = await memberOfGroups.NextPageRequest.GetAsync();
                        }
                        else
                        {
                            memberOfGroups = null;
                        }

                    } while (memberOfGroups != null);
                }                

                return groups;
            }
            catch (ServiceException e)
            {
                Trace.Fail("We could not get user groups: " + e.Error.Message);
                return null;
            }
        }

        public async Task<IList<DirectoryRole>> GetCurrentUserDirectoryRolesAsync()
        {
            IUserMemberOfCollectionWithReferencesPage memberOfDirectoryRoles = null;
            IList<DirectoryRole> DirectoryRoles = new List<DirectoryRole>();

            try
            {
                GraphServiceClient graphClient = this.GetAuthenticatedClientForUser();
                memberOfDirectoryRoles = await graphClient.Me.MemberOf.Request().GetAsync();

                if (memberOfDirectoryRoles != null)
                {
                    do
                    {
                        foreach (var directoryObject in memberOfDirectoryRoles.CurrentPage)
                        {
                            if (directoryObject is DirectoryRole)
                            {
                                DirectoryRole DirectoryRole = directoryObject as DirectoryRole;
                                Trace.WriteLine("Got DirectoryRole: " + DirectoryRole.Id);
                                DirectoryRoles.Add(DirectoryRole as DirectoryRole);
                            }
                        }
                        if (memberOfDirectoryRoles.NextPageRequest != null)
                        {
                            memberOfDirectoryRoles = await memberOfDirectoryRoles.NextPageRequest.GetAsync();
                        }
                        else
                        {
                            memberOfDirectoryRoles = null;
                        }

                    } while (memberOfDirectoryRoles != null);
                }

                return DirectoryRoles;
            }
            catch (ServiceException e)
            {
                Trace.Fail("We could not get user DirectoryRoles: " + e.Error.Message);
                return null;
            }
        }

        public async Task<IList<string>> GetCurrentUserGroupIdsAsync()
        {
            IList<string> groupObjectIds = new List<string>();
            var groups = await this.GetCurrentUserGroupsAsync();

            return groups.Select(x=>x.Id).ToList();
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