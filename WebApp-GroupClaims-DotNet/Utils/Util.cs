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