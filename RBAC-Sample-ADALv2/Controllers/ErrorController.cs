using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RBACSampleADALv2.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult ShowError()
        {
            ViewBag.ErrorMessage = Request.QueryString["errorMessage"]; 
            return View();
        }
    }
}