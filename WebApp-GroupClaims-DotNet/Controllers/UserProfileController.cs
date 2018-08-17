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

using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
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