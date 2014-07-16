using System.Web.Mvc;

namespace RBACSampleADALv2.Controllers
{
    public class ErrorController : Controller
    {
        /// <summary>
        ///     Shows an on-screen error message when the user attemps various
        ///     illegal actions.
        /// </summary>
        /// <returns>Generic error <see cref="View" />.</returns>
        public ActionResult ShowError()
        {
            ViewBag.ErrorMessage = Request.QueryString["errorMessage"];
            return View();
        }
    }
}