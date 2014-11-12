using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using ExpressionHelper = Microsoft.Azure.ActiveDirectory.GraphClient.ExpressionHelper;

//The following libraries were defined and added to this sample.
using WebAppGroupClaimsDotNet.Models;
using WebAppGroupClaimsDotNet.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using WebAppGroupClaimsDotNet.DAL;


namespace WebAppGroupClaimsDotNet.Controllers
{
    public class RolesController : Controller
    {
        ////////////////////////////////////////////////////////////////////
        //////// ACTIONS
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Show the current mappings of users and groups to each application role.
        /// Use AuthorizeAttribute to ensure only the role "Admin" can access the page.
        /// </summary>
        /// <returns>Role <see cref="View"/> with inputs to edit application role mappings.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Index()
        {
            // Get Existing Mappings from Roles.xml
            ViewBag.Message = "RoleMappings";
            List<RoleMapping> mappings = RolesDbHelper.GetAllRoleMappings();

            //Dictionary of <ObjectID, DisplayName> pairs
            var nameDict = new Dictionary<string, string>();

            // Construct the <ObjectID, DisplayName> Dictionary, Add Lists of Mappings to ViewData
            // for each type of role
            List<string> objectIds = new List<string>();
            foreach (RoleMapping mapping in mappings)
                objectIds.Add(mapping.ObjectId);
            
            try
            {
                List<User> users = new List<User>();
                List<Group> groups = new List<Group>();
                await GraphHelper.GetDirectoryObjects(objectIds, groups, users);
                foreach (User user in users)
                    nameDict[user.ObjectId] = user.DisplayName;
                foreach (Group group in groups)
                    nameDict[group.ObjectId] = group.DisplayName;
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
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while calling Graph API." });
            }
            
            ViewData["mappings"] = mappings;
            ViewData["nameDict"] = nameDict;
            ViewData["roles"] = Globals.Roles;
            ViewData["host"] = Request.Url.AbsoluteUri;
            ViewData["token"] = GraphHelper.AcquireToken(ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value);
            ViewData["tenant"] = ConfigHelper.Tenant;
            return View();
        }


        /// <summary>
        /// Adds a User/Group<-->Application Role mapping from user input form
        /// to roles.xml if it does not already exist.
        /// </summary>
        /// <param name="formCollection">The user input form, containing the UPN or GroupName
        /// of the object to grant a role.</param>
        /// <returns>A Redirect to the Roles page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult AssignRole(FormCollection formCollection)
        {
            // Check for an input name
            if (formCollection != null && formCollection["id"].Length > 0)
            {
                RolesDbHelper.AddRoleMapping(formCollection["id"], formCollection["role"]);
            }

            return RedirectToAction("Index", "Roles", null);
        }


        /// <summary>
        /// Removes a ObjectID<-->Application Role mapping from Roles.xml, based on input
        /// from the user.
        /// </summary>
        /// <param name="formCollection">The input from the user.</param>
        /// <returns>A redirect to the roles page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult RemoveRole(FormCollection formCollection)
        {
            // Remove role mapping assignments marked by checkboxes
            foreach (string key in formCollection.Keys)
            {
                if (formCollection[key].Equals("delete"))
                    RolesDbHelper.RemoveRoleMapping(Convert.ToInt32(key));
            }
            return RedirectToAction("Index", "Roles", null);
        }

        /// <summary>
        /// Used for the AadPickerLibrary that is used to search for users and groups.  Accepts a user input
        /// and a number of results to retreive, and queries the graphAPI for possbble matches.
        /// </summary>
        /// <returns>JSON containing query results ot the Javascript library.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async System.Threading.Tasks.Task<ActionResult> Search(string query, string token)
        {
            // Search for users based on user input.
            try
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return this.Content(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Json(new { error = "graph api error" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { error = "internal server error" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}