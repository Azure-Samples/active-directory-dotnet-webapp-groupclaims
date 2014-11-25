using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebAppGroupClaimsDotNet.Utils;

namespace WebAppGroupClaimsDotNet.Controllers
{

    // This controller can be used to test the overage claim feature.  The Create action
    // adds the signed-in user to 300 new groups, each named "Overage Test Group X". The
    // Delete action deletes every group whose displayName begins with "Overage".
    [Authorize]
    public class OverageTestController : Controller
    {
        public async Task<ActionResult> Create()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(new Uri(ConfigHelper.GraphServiceRoot), async () => { return GraphHelper.AcquireToken(userObjectId); });

            IPagedCollection<IUser> users = await graphClient.Users.Where(u => u.ObjectId.Equals(userObjectId)).ExecuteAsync();
            User user = (User)users.CurrentPage.First();

            for (int i = 0; i < 300; i++)
            { 
                string name = "Overage Test Group " + i.ToString();
                Group newGroup = new Group
                {
                    DisplayName = name,
                    SecurityEnabled = true,
                    MailEnabled = false,
                    Description = "Test Group for Overage Claim",
                    MailNickname = "TestGroup" + i.ToString(),
                };
                await graphClient.Groups.AddGroupAsync(newGroup);
                IPagedCollection<IGroup> thoseGroups = await graphClient.Groups.Where(g => g.DisplayName.Equals(name)).ExecuteAsync();
                Group thatGroup = (Group)thoseGroups.CurrentPage.First();
                graphClient.Context.AddLink(thatGroup, "members", user);
            }
            await graphClient.Context.SaveChangesAsync();

            return RedirectToAction("About", "Home", null);
        }

        public async Task<ActionResult> Delete()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(new Uri(ConfigHelper.GraphServiceRoot), async () => { return GraphHelper.AcquireToken(userObjectId); });

            IPagedCollection<IGroup> testGroups = await graphClient.Groups.Where(g => g.DisplayName.StartsWith("Overage")).ExecuteAsync();
            do
            {
                foreach (IGroup group in testGroups.CurrentPage.ToList())
                {
                    Group thisGroup = (Group)group;
                    await thisGroup.DeleteAsync();
                }
                testGroups = await testGroups.GetNextPageAsync();
            } while (testGroups != null);

            return RedirectToAction("About", "Home", null);
        }
    }
}