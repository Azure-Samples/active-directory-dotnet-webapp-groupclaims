using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Linq;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;

//The following libraries were defined and added to this sample.
using WebAppGroupClaimsDotNet.Utils;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Collections;
using System.Diagnostics;


namespace WebAppGroupClaimsDotNet.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Shows the generic MVC Get Started Home Page. Allows unauthenticated
        /// users to see the home page and click the sign-in link.
        /// </summary>
        /// <returns>Generic Home <see cref="View"/>.</returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Gets user specific RBAC information: The Security Groups the user belongs to
        /// And the application roles the user has been granted.
        /// </summary>
        /// <returns>The About <see cref="View"/>.</returns>
        [Authorize]
        public async Task<ActionResult> About()
        {
            var myRoles = new List<String>();
            var myGroups = new List<Group>();
            var myDirectoryRoles = new List<DirectoryRole>();

            // Check if the user has been granted each application role.
            foreach (string str in Globals.Roles)
            {
                if (User.IsInRole(str))
                    myRoles.Add(str);
            }

            try {

                List<string> objectIds = new List<string>();
                foreach (Claim claim in ClaimsPrincipal.Current.FindAll("groups").ToList())
                    objectIds.Add(claim.Value);
                await GraphHelper.GetDirectoryObjects(objectIds, myGroups, myDirectoryRoles);
            }
            catch (AdalException e)
            {
                // If the user doesn't have an access token, they need to re-authorize
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while acquiring token." });
            }
            catch (Exception e)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = e.Message });
            }


            // For the security groups the user is a member of, get the DisplayName
            ViewData["myRoles"] = myRoles;
            ViewData["myGroups"] = myGroups;
            ViewData["myDirectoryRoles"] = myDirectoryRoles;
            ViewData["overageOccurred"] = (ClaimsPrincipal.Current.FindFirst("_claim_names") != null && (System.Web.Helpers.Json.Decode(ClaimsPrincipal.Current.FindFirst("_claim_names").Value)).groups != null);
            return View();
        }
    }
}