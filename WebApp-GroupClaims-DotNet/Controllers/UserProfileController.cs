using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebApp_GroupClaims_DotNet.Models;
using WebApp_GroupClaims_DotNet.Utils;

namespace WebApp_GroupClaims_DotNet.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        // GET: UserProfile
        public async Task<ActionResult> Index()
        {
            try
            {
                MSGraphClient msGraphClient = new MSGraphClient(AppConfig.Authority, new ADALTokenCache(Util.GetSignedInUsersObjectIdFromClaims()));

                User user = await msGraphClient.GetMeAsync();
                UserGroupsAndDirectoryRoles userGroupsAndDirectoryRoles = await msGraphClient.GetCurrentUserGroupsAndRolesAsync();

                //IList<Group> groups = await msGraphClient.GetCurrentUserGroupsAsync();
                //IList<DirectoryRole> directoryRoles = await msGraphClient.GetCurrentUserDirectoryRolesAsync();

                ViewData["overageOccurred"] = userGroupsAndDirectoryRoles.HasOverageClaim;
                ViewData["myGroups"] = userGroupsAndDirectoryRoles.Groups;
                ViewData["myDirectoryRoles"] = userGroupsAndDirectoryRoles.DirectoryRoles;
                return View(user);
            }
            catch (AdalException)
            {
                // Return to error page.
                return View("Error");
            }
            // if the above failed, the user needs to explicitly re-authenticate for the app to obtain the required token
            catch (Exception)
            {
                return View("Relogin");
            }
        }
    }
}