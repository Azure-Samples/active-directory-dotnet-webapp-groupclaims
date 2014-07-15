using RBACSampleADALv2.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RBACSampleADALv2.Controllers
{
    public class TasksController : Controller
    {
        // GET: Tasks
        [HttpGet]
        [Authorize(Roles = "Admin, Observer, Writer, Approver")]
        public ActionResult Index()
        {
            ViewBag.Message = "Tasks";
            ViewData["tasks"] = XmlHelper.GetTaskElemsFromXml();
            return View();
        }

        //TODO: Better Comments, Clean Code

        [HttpPost]
        [Authorize(Roles = "Admin, Writer, Approver")] //TODO: test out functionality
        public ActionResult TaskSubmit(FormCollection formCollection)
        {
            ActionResult result = RedirectToAction("Index", "Tasks");
            if (User.IsInRole("Admin") || User.IsInRole("Writer"))
            {
                //add new task
                if (formCollection["newTask"] != null && formCollection["newTask"].Length != 0)
                {
                    XmlHelper.AppendTaskElemToXml(formCollection);
                }
            }

            if (User.IsInRole("Admin") || User.IsInRole("Approver"))
            {
                //change status of existing task
                XmlHelper.ChangeTaskAttribute(formCollection);
            }

            return result;
        }
    }
}