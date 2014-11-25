using System;
using System.Web;
using System.Web.Mvc;

// The following libraries were added to the sample.
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;


namespace WebAppGroupClaimsDotNet.Controllers
{
    public class GraphProxyController : Controller
    {
        /// <summary>
        /// Used for the AadPickerLibrary that is used to search for users and groups.  Accepts a user input
        /// and a number of results to retreive, and queries the graphAPI for possbble matches.
        /// </summary>
        /// <returns>JSON containing query results ot the Javascript library.</returns>
        [HttpPost]
        [Authorize]
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