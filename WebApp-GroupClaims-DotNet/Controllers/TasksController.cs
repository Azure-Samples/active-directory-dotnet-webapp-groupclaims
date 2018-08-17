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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebApp_GroupClaims_DotNet.Data;
using WebApp_GroupClaims_DotNet.Models;
using WebApp_GroupClaims_DotNet.Utils;

namespace WebApp_GroupClaims_DotNet.Controllers
{
    public class TasksController : Controller
    {
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Index()
        {
            try
            {
                // Get All Tasks User Can View
                ClaimsIdentity userClaimsId = ClaimsPrincipal.Current.Identity as ClaimsIdentity;
                UserGroupsAndDirectoryRoles userGroupsAndDirectoryRoles = await TokenHelper.GetUsersGroupsAsync(ClaimsPrincipal.Current);

                List<string> userGroupsAndId = userGroupsAndDirectoryRoles.GroupIds;

                string userObjectId = Util.GetSignedInUsersObjectIdFromClaims();
                userGroupsAndId.Add(userObjectId);

                ViewData["tasks"] = TasksDbHelper.GetAllTasks(userGroupsAndId);
                ViewData["userId"] = userObjectId;
                return View();
            }
            catch (Exception e)
            {
                // Catch Both ADAL Exceptions and Web Exceptions
                return RedirectToAction("ShowError", "Error", new { errorMessage = e.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public ActionResult TaskSubmit(FormCollection formCollection)
        {
            // Create a new task
            if (formCollection["newTask"] != null && formCollection["newTask"].Length != 0)
            {
                TasksDbHelper.AddTask(formCollection["newTask"],
                    ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value,
                    ClaimsPrincipal.Current.FindFirst(Globals.GivennameClaimType).Value + ' '
                    + ClaimsPrincipal.Current.FindFirst(Globals.SurnameClaimType).Value);
            }

            // Change status of existing task
            if (formCollection["updateTasks"] != null)
            {
                foreach (string key in formCollection.Keys)
                {
                    if (key.StartsWith("task-id:"))
                        TasksDbHelper.UpdateTask(Convert.ToInt32(key.Substring(key.IndexOf(':') + 1)), formCollection[key]);
                }
            }

            // Delete a Task
            if (formCollection["delete"] != null && formCollection["delete"].Length > 0)
                TasksDbHelper.DeleteTask(Convert.ToInt32(formCollection["delete"]));

            return RedirectToAction("Index", "Tasks");
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Share(string id)
        {
            AuthenticationHelper authHelper = new AuthenticationHelper(AppConfig.Authority, new ADALTokenCache(Util.GetSignedInUsersObjectIdFromClaims()));

            // Values Needed for the People Picker
            ViewData["tenant"] = AppConfig.TenantId;
            ViewData["token"] = await authHelper.GetAccessTokenForUserAsync(AppConfig.GraphResourceId, AppConfig.PostLogoutRedirectUri);

            UserGroupsAndDirectoryRoles userGroupsAndDirectoryRoles = await TokenHelper.GetUsersGroupsAsync(ClaimsPrincipal.Current);
            List<string> userGroupsAndId = userGroupsAndDirectoryRoles.GroupIds;

            string userObjectId = Util.GetSignedInUsersObjectIdFromClaims();
            userGroupsAndId.Add(userObjectId);

            ViewData["tasks"] = TasksDbHelper.GetAllTasks(userGroupsAndId);            
            ViewData["userId"] = userObjectId;
                        
            // Get the task details
            WebApp_GroupClaims_DotNet.Models.Task task = TasksDbHelper.GetTask(Convert.ToInt32(id));
            if (task == null)
            {
                RedirectToAction("ShowError", "Error", new { message = "Task Not Found in DB." });
            }

            ViewData["shares"] = task.SharedWith.ToList();
            ViewData["taskText"] = task.TaskText;
            ViewData["taskId"] = task.TaskID;
            
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Share(int taskId, string objectId, string displayName, string delete, string shareTasks)
        {
            // If the share button was clicked, share the task with the user or group
            if (shareTasks != null && objectId != null && objectId != string.Empty && displayName != null && displayName != string.Empty)
            {
                TasksDbHelper.AddShare(taskId, objectId, displayName);
            }

            // If a delete button was clicked, remove the share from the task
            if (delete != null && delete.Length > 0)
            {
                TasksDbHelper.DeleteShare(taskId, delete);
            }

            return RedirectToAction("Share", new { id = taskId });
        }
    }
}