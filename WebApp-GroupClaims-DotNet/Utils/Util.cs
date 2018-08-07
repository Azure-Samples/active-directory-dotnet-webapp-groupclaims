using System;
using System.Security.Claims;
using WebApp_GroupClaims_DotNet.Utils;

namespace WebApp_GroupClaims_DotNet.Models
{
    public class Util
    {
        public static string EnsureTrailingSlash(string value)
        {
            if (value == null)
                value = String.Empty;

            if (!value.EndsWith("/", StringComparison.Ordinal))
                return value + "/";

            return value;
        }

        public static string GetSignedInUsersIdFromClaims()
        {
            string signedInUsersId = string.Empty;

            if (ClaimsPrincipal.Current != null)
            {
                signedInUsersId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            }            

            return signedInUsersId;
        }

        public static string GetSignedInUsersObjectIdFromClaims()
        {
            string signedInUsersId = string.Empty;

            if (ClaimsPrincipal.Current != null)
            {
                signedInUsersId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
            }

            return signedInUsersId;
        }
    }
}