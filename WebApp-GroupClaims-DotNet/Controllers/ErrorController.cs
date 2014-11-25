using System.Web.Mvc;
using System.Web;

// The following libraries were added to this sample.
using Microsoft.Owin.Security;

namespace WebAppGroupClaimsDotNet.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult ShowError(string errorMessage, string signIn)
        {
            ViewBag.SignIn = signIn;
            ViewBag.ErrorMessage = errorMessage;
            return View();
        }

        public ActionResult Reauth(string redirectUri)
        {
            ViewBag.RedirectUri = redirectUri;
            return View();
        }
    }
}