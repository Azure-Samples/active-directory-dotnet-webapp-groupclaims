using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using WebApp_GroupClaims_DotNet.Models;

namespace WebApp_GroupClaims_DotNet.Utils
{
    public class TokenHelper
    {
        public static IList<string> GetUsersGroups(ClaimsPrincipal subject, out bool hadOverageClaim)
        {
            hadOverageClaim = HasGroupsOverageClaim(subject);
            ClaimsIdentity userClaimsId = subject.Identity as ClaimsIdentity;

            if (hadOverageClaim)
            {
                string signedInUserId = subject.FindFirst(ClaimTypes.NameIdentifier).Value;
                MSGraphClient graphClient = new MSGraphClient(AppConfig.Authority, new ADALTokenCache(signedInUserId));
                return graphClient.GetCurrentUserGroupIdsAsync().Result;
            }
            else
            {
                return userClaimsId.FindAll(SubjectAttribute.Groups).Select(c => c.Value).ToList();
            }
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