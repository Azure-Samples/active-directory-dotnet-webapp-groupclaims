using System;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

//The following libraries were defined and added to this sample.
using WebAppGroupClaimsDotNet.Utils;


namespace WebAppGroupClaimsDotNet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> About()
        {
            var myGroups = new List<Group>();
            var myDirectoryRoles = new List<DirectoryRole>();

            try
            {
                ClaimsIdentity claimsId = ClaimsPrincipal.Current.Identity as ClaimsIdentity;
                List<string> objectIds = await ClaimHelper.GetGroups(claimsId);
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

            ViewData["myGroups"] = myGroups;
            ViewData["myDirectoryRoles"] = myDirectoryRoles;
            ViewData["overageOccurred"] = (ClaimsPrincipal.Current.FindFirst("_claim_names") != null && 
                (System.Web.Helpers.Json.Decode(ClaimsPrincipal.Current.FindFirst("_claim_names").Value)).groups != null);
            return View();
        }
    }
}