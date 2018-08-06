using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
    }
}